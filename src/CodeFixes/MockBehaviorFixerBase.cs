using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.CodeFixes;

/// <summary>
/// Provides shared registration logic for MockBehavior code fix providers.
/// </summary>
public abstract class MockBehaviorFixerBase : CodeFixProvider
{
    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SyntaxNode? nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);

        if (!context.TryGetEditProperties(out DiagnosticEditProperties? editProperties))
        {
            return;
        }

        if (nodeToFix is null)
        {
            return;
        }

        RegisterFixes(context, nodeToFix, editProperties);
    }

    private protected abstract void RegisterFixes(
        CodeFixContext context,
        SyntaxNode nodeToFix,
        DiagnosticEditProperties editProperties);
}
