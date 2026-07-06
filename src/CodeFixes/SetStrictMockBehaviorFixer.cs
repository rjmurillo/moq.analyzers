using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.SetStrictMockBehavior"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetStrictMockBehaviorFixer))]
[Shared]
public class SetStrictMockBehaviorFixer : MockBehaviorFixerBase
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.SetStrictMockBehavior);

    /// <inheritdoc />
    private protected override void RegisterFixes(
        CodeFixContext context,
        SyntaxNode nodeToFix,
        DiagnosticEditProperties editProperties)
    {
        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Strict)", context.Document, nodeToFix, BehaviorType.Strict, editProperties.TypeOfEdit, editProperties.EditPosition, replaceRequiresDefaultReference: false), context.Diagnostics);
    }
}
