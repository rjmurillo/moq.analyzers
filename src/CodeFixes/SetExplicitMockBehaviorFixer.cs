using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for SetExplicitMockBehaviorAnalyzer (Moq1400).
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

    private enum EditType
    {
        Insert,
        Replace,
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

        EditType editType = (EditType)Enum.Parse(typeof(EditType), context.Diagnostics[0].Properties["EditType"]); // TODO: Clean up
        int position = int.Parse(context.Diagnostics[0].Properties["EditPosition"]); // TODO: Clean up

        if (nodeToFix is null)
        {
            return;
        }

        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Loose)", context.Document, nodeToFix, BehaviorType.Loose, editType, position), context.Diagnostics);
        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Strict)", context.Document, nodeToFix, BehaviorType.Strict, editType, position), context.Diagnostics);
    }

    private sealed class SetExplicitMockBehaviorCodeAction : CodeAction
    {
        private readonly Document _document;
        private readonly SyntaxNode _nodeToFix;
        private readonly BehaviorType _behaviorType;
        private readonly EditType _editType;
        private readonly int _position;

        public SetExplicitMockBehaviorCodeAction(string title, Document document, SyntaxNode nodeToFix, BehaviorType behaviorType, EditType editType, int position)
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
            MoqKnownSymbols knownSymbols = new(editor.SemanticModel.Compilation);

            if (knownSymbols.MockBehavior is null
                || knownSymbols.MockBehaviorDefault is null
                || knownSymbols.MockBehaviorLoose is null
                || knownSymbols.MockBehaviorStrict is null)
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
                EditType.Insert => editor.Generator.InsertArguments(_nodeToFix, _position, argument),
                EditType.Replace => editor.Generator.ReplaceArgument(_nodeToFix, _position, argument),
                _ => throw new InvalidOperationException(),
            };

            editor.ReplaceNode(_nodeToFix, newNode.WithAdditionalAnnotations(Simplifier.Annotation));
            return editor.GetChangedDocument();
        }
    }
}

internal static class SyntaxGeneratorExtensions
{
    public static SyntaxNode MemberAccessExpression(this SyntaxGenerator generator, IFieldSymbol fieldSymbol)
    {
        return generator.MemberAccessExpression(generator.TypeExpression(fieldSymbol.Type), generator.IdentifierName(fieldSymbol.Name));
    }

    public static SyntaxNode InsertArguments(this SyntaxGenerator generator, SyntaxNode syntax, int index, params SyntaxNode[] items)
    {
        if (syntax is InvocationExpressionSyntax invocation)
        {
            if (items.Any(item => item is not ArgumentSyntax))
            {
                throw new ArgumentException("Must all be of type ArgumentSyntax", nameof(items));
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            arguments = arguments.InsertRange(index, items.OfType<ArgumentSyntax>());

            syntax = syntax.ReplaceNode(invocation.ArgumentList, invocation.ArgumentList.WithArguments(arguments));

            return syntax;
        }

        if (syntax is ObjectCreationExpressionSyntax creation)
        {
            if (items.Any(item => item is not ArgumentSyntax))
            {
                throw new ArgumentException("Must all be of type ArgumentSyntax", nameof(items));
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList.Arguments;

            arguments = arguments.InsertRange(index, items.OfType<ArgumentSyntax>());

            syntax = syntax.ReplaceNode(creation.ArgumentList, creation.ArgumentList.WithArguments(arguments));

            return syntax;
        }

        throw new ArgumentException($"Must be of type {nameof(InvocationExpressionSyntax)} but is of type {syntax.GetType().Name}", nameof(syntax));
    }

    public static SyntaxNode ReplaceArgument(this SyntaxGenerator generator, SyntaxNode syntax, int index, SyntaxNode item) // TODO: Make this range-based
    {
        if (syntax is InvocationExpressionSyntax invocation)
        {
            if (item is not ArgumentSyntax argument)
            {
                throw new ArgumentException("Must be of type ArgumentSyntax", nameof(item));
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            arguments = arguments.RemoveAt(index).Insert(index, argument);

            syntax = syntax.ReplaceNode(invocation.ArgumentList, invocation.ArgumentList.WithArguments(arguments));

            return syntax;
        }

        if (syntax is ObjectCreationExpressionSyntax creation)
        {
            if (item is not ArgumentSyntax argument)
            {
                throw new ArgumentException("Must be of type ArgumentSyntax", nameof(item));
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList.Arguments;

            arguments = arguments.RemoveAt(index).Insert(index, argument);

            syntax = syntax.ReplaceNode(creation.ArgumentList, creation.ArgumentList.WithArguments(arguments));

            return syntax;
        }

        throw new ArgumentException($"Must be of type {nameof(InvocationExpressionSyntax)} but is of type {syntax.GetType().Name}", nameof(syntax));
    }
}
