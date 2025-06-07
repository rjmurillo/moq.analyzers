using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.SequenceSetupAfterStandardSetup"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SequenceSetupAfterStandardSetupFixer))]
[Shared]
public class SequenceSetupAfterStandardSetupFixer : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.SequenceSetupAfterStandardSetup);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        SyntaxNode? nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (nodeToFix is not InvocationExpressionSyntax invocationExpression)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Replace SetupSequence with Setup",
                cancellationToken => ReplaceSetupSequenceAsync(context.Document, root, invocationExpression),
                "Replace SetupSequence with Setup"),
            diagnostic);
    }

    private static Task<Document> ReplaceSetupSequenceAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocationExpression)
    {
        // Replace "SetupSequence" with "Setup" in the method name
        if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is IdentifierNameSyntax or GenericNameSyntax)
        {
            SimpleNameSyntax newMethodName = SyntaxFactory.IdentifierName("Setup");

            // Preserve generic type arguments if present
            if (memberAccess.Name is GenericNameSyntax genericName)
            {
                newMethodName = SyntaxFactory.GenericName(SyntaxFactory.Identifier("Setup"))
                    .WithTypeArgumentList(genericName.TypeArgumentList);
            }

            MemberAccessExpressionSyntax newMemberAccess = memberAccess.WithName(newMethodName);
            InvocationExpressionSyntax newInvocation = invocationExpression.WithExpression(newMemberAccess);

            SyntaxNode newRoot = root.ReplaceNode(invocationExpression, newInvocation);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        return Task.FromResult(document);
    }
}
