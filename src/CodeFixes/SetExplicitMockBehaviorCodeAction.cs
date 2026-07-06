using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Moq.CodeFixes;

internal sealed class SetExplicitMockBehaviorCodeAction : CodeAction
{
    private readonly Document _document;
    private readonly SyntaxNode _nodeToFix;
    private readonly BehaviorType _behaviorType;
    private readonly DiagnosticEditProperties.EditType _editType;
    private readonly int _position;
    private readonly bool _replaceRequiresDefaultReference;

    public SetExplicitMockBehaviorCodeAction(
        string title,
        Document document,
        SyntaxNode nodeToFix,
        BehaviorType behaviorType,
        DiagnosticEditProperties.EditType editType,
        int position,
        bool replaceRequiresDefaultReference)
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
        _replaceRequiresDefaultReference = replaceRequiresDefaultReference;
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
        // original document instead of applying a wrong edit or letting the argument rewriters throw.
        if (!CanApplyEdit(editor.SemanticModel, knownSymbols.MockBehavior, knownSymbols.MockBehaviorDefault))
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

    private static bool ContainsMockBehaviorArgument(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> candidates, ISymbol mockBehaviorType)
    {
        foreach (ArgumentSyntax argument in candidates)
        {
            // Use the converted type, not the expression type, so an argument that binds to the
            // MockBehavior parameter through an implicit conversion (for example the literal 0 or
            // default) is still recognized as an explicit MockBehavior argument.
            ITypeSymbol? argumentType = semanticModel.GetTypeInfo(argument.Expression).ConvertedType;
            if (SymbolEqualityComparer.Default.Equals(argumentType, mockBehaviorType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMockBehaviorDefaultReference(SemanticModel semanticModel, ArgumentSyntax argument, IFieldSymbol mockBehaviorDefault)
    {
        IOperation? operation = semanticModel.GetOperation(argument.Expression);
        System.Diagnostics.Debug.Assert(operation is not null, "A valid argument expression should produce an operation.");

        return operation!.DescendantsAndSelf().OfType<IFieldReferenceOperation>().Any(field => field.Member.IsInstanceOf(mockBehaviorDefault));
    }

    /// <summary>
    /// Validates that the edit recorded on the diagnostic can be applied safely to the current node.
    /// Guards against a stale lightbulb where the document changed after analysis: the node must still
    /// be an invocation or object creation, <see cref="_position"/> must be in range for the requested
    /// edit, an <see cref="DiagnosticEditProperties.EditType.Insert"/> must not duplicate a
    /// <c>MockBehavior</c> argument the user added after the diagnostic was produced, and an explicit
    /// behavior fix <see cref="DiagnosticEditProperties.EditType.Replace"/> must still point at an
    /// argument containing <c>MockBehavior.Default</c>.
    /// </summary>
    /// <param name="semanticModel">The semantic model used to resolve argument types.</param>
    /// <param name="mockBehaviorType">The <c>MockBehavior</c> type symbol.</param>
    /// <param name="mockBehaviorDefault">The <c>MockBehavior.Default</c> field symbol.</param>
    /// <returns><see langword="true"/> when the edit can be applied safely; otherwise <see langword="false"/>.</returns>
    private bool CanApplyEdit(SemanticModel semanticModel, ISymbol mockBehaviorType, IFieldSymbol mockBehaviorDefault)
    {
        if (!TryGetArgumentList(out SeparatedSyntaxList<ArgumentSyntax> arguments))
        {
            return false;
        }

        return _editType switch
        {
            // Insert is emitted only when the MockBehavior parameter is defaulted (no explicit argument).
            // If an explicit MockBehavior argument now exists, the diagnostic is stale; inserting another
            // would duplicate it.
            DiagnosticEditProperties.EditType.Insert =>
                _position <= arguments.Count
                && !ContainsMockBehaviorArgument(semanticModel, arguments, mockBehaviorType),
            DiagnosticEditProperties.EditType.Replace =>
                _position < arguments.Count
                && (!_replaceRequiresDefaultReference
                    || ContainsMockBehaviorDefaultReference(semanticModel, arguments[_position], mockBehaviorDefault)),
            _ => false,
        };
    }

    private bool TryGetArgumentList(out SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        switch (_nodeToFix)
        {
            case InvocationExpressionSyntax invocation:
                arguments = invocation.ArgumentList.Arguments;
                return true;
            case BaseObjectCreationExpressionSyntax creation:
                arguments = creation.ArgumentList?.Arguments ?? default;
                return true;
            default:
                arguments = default;
                return false;
        }
    }
}
