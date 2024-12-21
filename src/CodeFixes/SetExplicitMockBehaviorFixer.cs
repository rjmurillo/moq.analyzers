using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.SetExplicitMockBehavior"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetExplicitMockBehaviorFixer))]
[Shared]
public class SetExplicitMockBehaviorFixer : CodeFixProvider
{
    private enum BehaviorType
    {
        Loose,
        Strict,
    }

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

    private sealed class SetExplicitMockBehaviorCodeAction : CodeAction
    {
        private readonly Document _document;
        private readonly SyntaxNode _nodeToFix;
        private readonly BehaviorType _behaviorType;
        private readonly DiagnosticEditProperties.EditType _editType;
        private readonly int _position;

        public SetExplicitMockBehaviorCodeAction(string title, Document document, SyntaxNode nodeToFix, BehaviorType behaviorType, DiagnosticEditProperties.EditType editType, int position)
        {
            Title = title;
            _document = document;
            _nodeToFix = nodeToFix;
            _behaviorType = behaviorType;
            _editType = editType;
            _position = position;
        }

        public override string Title { get; }

        public override string? EquivalenceKey => Title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            DocumentEditor editor = await DocumentEditor.CreateAsync(_document, cancellationToken).ConfigureAwait(false);
            SemanticModel? model = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            IOperation? operation = model?.GetOperation(_nodeToFix, cancellationToken);

            MoqKnownSymbols knownSymbols = new(editor.SemanticModel.Compilation);

            if (knownSymbols.MockBehavior is null
                || knownSymbols.MockBehaviorDefault is null
                || knownSymbols.MockBehaviorLoose is null
                || knownSymbols.MockBehaviorStrict is null
                || operation is null)
            {
                return _document;
            }

            SyntaxNode behavior = _behaviorType switch
            {
                BehaviorType.Loose => editor.Generator.MemberAccessExpression(knownSymbols.MockBehaviorLoose),
                BehaviorType.Strict => editor.Generator.MemberAccessExpression(knownSymbols.MockBehaviorStrict),
                _ => throw new InvalidOperationException(),
            };

            SyntaxNode argument = editor.Generator.Argument(behavior);

            SyntaxNode newNode = _editType switch
            {
                DiagnosticEditProperties.EditType.Insert => editor.Generator.InsertArguments(operation, _position, argument),
                DiagnosticEditProperties.EditType.Replace => editor.Generator.ReplaceArgument(operation, _position, argument),
                _ => throw new InvalidOperationException(),
            };

            editor.ReplaceNode(_nodeToFix, newNode.WithAdditionalAnnotations(Simplifier.Annotation));
            return editor.GetChangedDocument();
        }
    }
}
