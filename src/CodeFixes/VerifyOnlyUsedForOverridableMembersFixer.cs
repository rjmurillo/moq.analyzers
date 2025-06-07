using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.VerifyOnlyUsedForOverridableMembers"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VerifyOnlyUsedForOverridableMembersFixer))]
[Shared]
public class VerifyOnlyUsedForOverridableMembersFixer : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.VerifyOnlyUsedForOverridableMembers);

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
                "Remove Verify call",
                cancellationToken => RemoveVerifyCallAsync(context.Document, root, invocationExpression),
                "Remove Verify call"),
            diagnostic);
    }

    private static Task<Document> RemoveVerifyCallAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocationExpression)
    {
        // Find the containing statement
        ExpressionStatementSyntax? containingStatement = invocationExpression.Ancestors().OfType<ExpressionStatementSyntax>().FirstOrDefault();

        if (containingStatement == null)
        {
            return Task.FromResult(document);
        }

        // Remove the entire statement
        SyntaxNode? newRoot = root.RemoveNode(containingStatement, SyntaxRemoveOptions.KeepNoTrivia);
        return Task.FromResult(document.WithSyntaxRoot(newRoot ?? root));
    }
}
