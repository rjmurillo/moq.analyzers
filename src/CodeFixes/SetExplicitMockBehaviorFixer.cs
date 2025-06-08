using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.SetExplicitMockBehavior"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetExplicitMockBehaviorFixer))]
[Shared]
public class SetExplicitMockBehaviorFixer : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.SetExplicitMockBehavior);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
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

        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Loose)", context.Document, nodeToFix, BehaviorType.Loose, editProperties.TypeOfEdit, editProperties.EditPosition), context.Diagnostics);
        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Strict)", context.Document, nodeToFix, BehaviorType.Strict, editProperties.TypeOfEdit, editProperties.EditPosition), context.Diagnostics);
    }
}
