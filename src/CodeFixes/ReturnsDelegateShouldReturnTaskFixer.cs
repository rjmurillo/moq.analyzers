using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod"/> (Moq1208).
/// Replaces <c>.Returns(delegate)</c> with <c>.ReturnsAsync(delegate)</c> when the
/// mocked method is async and the delegate returns the unwrapped type.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnsDelegateShouldReturnTaskFixer))]
[Shared]
public sealed class ReturnsDelegateShouldReturnTaskFixer : CodeFixProvider
{
    private static readonly string Title = "Use ReturnsAsync instead of Returns";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics[0];

        // The diagnostic span starts at the "Returns" identifier. Use FindToken to land
        // on that token, then walk up to the enclosing MemberAccessExpressionSyntax.
        SyntaxToken token = root.FindToken(diagnostic.Location.SourceSpan.Start);
        MemberAccessExpressionSyntax? memberAccess = token.Parent?.FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
        if (memberAccess == null)
        {
            return;
        }

        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "Returns", StringComparison.Ordinal))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: _ => ReplaceReturnsWithReturnsAsync(context.Document, root, memberAccess),
                equivalenceKey: Title),
            diagnostic);
    }

    private static Task<Document> ReplaceReturnsWithReturnsAsync(
        Document document,
        SyntaxNode root,
        MemberAccessExpressionSyntax memberAccess)
    {
        SimpleNameSyntax oldName = memberAccess.Name;
        IdentifierNameSyntax newName = SyntaxFactory.IdentifierName("ReturnsAsync")
            .WithLeadingTrivia(oldName.GetLeadingTrivia())
            .WithTrailingTrivia(oldName.GetTrailingTrivia());

        MemberAccessExpressionSyntax newMemberAccess = memberAccess.WithName(newName);
        SyntaxNode newRoot = root.ReplaceNode(memberAccess, newMemberAccess);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
