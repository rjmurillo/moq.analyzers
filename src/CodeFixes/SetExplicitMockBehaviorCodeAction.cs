using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Moq.CodeFixes;

internal sealed class SetExplicitMockBehaviorCodeAction : CodeAction
{
    private readonly Document _document;
    private readonly SyntaxNode _nodeToFix;
    private readonly BehaviorType _behaviorType;
    private readonly DiagnosticEditProperties.EditType _editType;
    private readonly int _position;

    public SetExplicitMockBehaviorCodeAction(string title, Document document, SyntaxNode nodeToFix, BehaviorType behaviorType, DiagnosticEditProperties.EditType editType, int position)
    {
        if (title is null)
        {
            throw new ArgumentNullException(nameof(title));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (nodeToFix is null)
        {
            throw new ArgumentNullException(nameof(nodeToFix));
        }

        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} must be non-negative.");
        }

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

        // Defense in depth: the edit properties attached to the diagnostic describe the argument-list
        // shape the analyzer saw at analysis time. If the document changed between analysis and fix
        // application (stale lightbulb), the resolved node may no longer match that shape. Return the
        // original document instead of letting the argument rewriters throw.
        if (!IsEditShapeValid())
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
            DiagnosticEditProperties.EditType.Insert => editor.Generator.InsertArguments(_nodeToFix, _position, argument),
            DiagnosticEditProperties.EditType.Replace => editor.Generator.ReplaceArgument(_nodeToFix, _position, argument),
            _ => throw new InvalidOperationException(),
        };

        editor.ReplaceNode(_nodeToFix, newNode.WithAdditionalAnnotations(Simplifier.Annotation));
        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Validates that <see cref="_nodeToFix"/> is a node kind the argument rewriters understand
    /// (invocation or object creation) and that <see cref="_position"/> is within range for the
    /// requested edit against the node's current argument count.
    /// </summary>
    /// <returns><see langword="true"/> when the edit can be applied safely; otherwise <see langword="false"/>.</returns>
    private bool IsEditShapeValid()
    {
        int argumentCount = _nodeToFix switch
        {
            InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments.Count,
            BaseObjectCreationExpressionSyntax creation => creation.ArgumentList?.Arguments.Count ?? 0,
            _ => -1,
        };

        if (argumentCount < 0)
        {
            return false;
        }

        return _editType switch
        {
            DiagnosticEditProperties.EditType.Insert => _position <= argumentCount,
            DiagnosticEditProperties.EditType.Replace => _position < argumentCount,
            _ => false,
        };
    }
}
