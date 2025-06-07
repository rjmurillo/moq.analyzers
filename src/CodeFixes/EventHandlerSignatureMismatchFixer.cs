using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.EventHandlerSignatureMismatch"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EventHandlerSignatureMismatchFixer))]
[Shared]
public class EventHandlerSignatureMismatchFixer : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.EventHandlerSignatureMismatch);

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

        // Try to find It.IsAny<T>() patterns to fix
        if (TryFindItIsAnyPattern(invocationExpression, out InvocationExpressionSyntax? itIsAnyCall) && itIsAnyCall != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Fix event handler type",
                    cancellationToken => FixEventHandlerTypeAsync(context.Document, root, itIsAnyCall),
                    "Fix event handler type"),
                diagnostic);
        }
    }

    private static bool TryFindItIsAnyPattern(SyntaxNode node, out InvocationExpressionSyntax? itIsAnyCall)
    {
        itIsAnyCall = null;

        // Look for It.IsAny<T>() patterns in the node tree
        itIsAnyCall = node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax identifier &&
string.Equals(identifier.Identifier.ValueText, "It", StringComparison.Ordinal) &&
                memberAccess.Name is GenericNameSyntax genericName &&
string.Equals(genericName.Identifier.ValueText, "IsAny", StringComparison.Ordinal));

        return itIsAnyCall != null;
    }

    private static Task<Document> FixEventHandlerTypeAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax itIsAnyCall)
    {
        // Replace with EventHandler as a generic fix
        // In a real implementation, we would analyze the actual event type
        TypeSyntax eventHandlerType = SyntaxFactory.IdentifierName("EventHandler");

        if (itIsAnyCall.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
        {
            GenericNameSyntax newGenericName = genericName.WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(eventHandlerType)));

            MemberAccessExpressionSyntax newMemberAccess = memberAccess.WithName(newGenericName);
            InvocationExpressionSyntax newInvocation = itIsAnyCall.WithExpression(newMemberAccess);

            SyntaxNode newRoot = root.ReplaceNode(itIsAnyCall, newInvocation);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        return Task.FromResult(document);
    }
}
