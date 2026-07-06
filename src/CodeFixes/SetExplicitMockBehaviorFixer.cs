using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.SetExplicitMockBehavior"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetExplicitMockBehaviorFixer))]
[Shared]
public class SetExplicitMockBehaviorFixer : MockBehaviorFixerBase
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.SetExplicitMockBehavior);

    /// <inheritdoc />
    private protected override void RegisterFixes(
        CodeFixContext context,
        SyntaxNode nodeToFix,
        DiagnosticEditProperties editProperties)
    {
        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Loose)", context.Document, nodeToFix, BehaviorType.Loose, editProperties.TypeOfEdit, editProperties.EditPosition, replaceRequiresDefaultReference: true), context.Diagnostics);
        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Strict)", context.Document, nodeToFix, BehaviorType.Strict, editProperties.TypeOfEdit, editProperties.EditPosition, replaceRequiresDefaultReference: true), context.Diagnostics);
    }
}
