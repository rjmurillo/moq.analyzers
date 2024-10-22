using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for SetExplicitMockBehaviorAnalyzer (Moq1400).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetExplicitMockBehaviorCodeFix))]
[Shared]
public class SetExplicitMockBehaviorCodeFix : CodeFixProvider
{
    private static readonly ArgumentSyntax LooseSyntax = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(WellKnownTypeNames.MockBehavior), SyntaxFactory.IdentifierName(WellKnownTypeNames.Loose)));
    private static readonly ArgumentSyntax StrictSyntax = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(WellKnownTypeNames.MockBehavior), SyntaxFactory.IdentifierName(WellKnownTypeNames.Strict)));

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.SetExplicitMockBehavior);

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SyntaxNode? nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);

        if (nodeToFix is null)
        {
            return;
        }

        context.RegisterCodeFix(CodeAction.Create("Set MockBehavior (Loose)", ct => AddMockBehaviorAsync(context.Document, nodeToFix, LooseSyntax, ct), equivalenceKey: "Set MockBehavior (Loose)"), context.Diagnostics);
        context.RegisterCodeFix(CodeAction.Create("Set MockBehavior (Strict)", ct => AddMockBehaviorAsync(context.Document, nodeToFix, StrictSyntax, ct), equivalenceKey: "Set MockBehavior (Strict)"), context.Diagnostics);
    }

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    private static async Task<Document> AddMockBehaviorAsync(Document document, SyntaxNode nodeToFix, ArgumentSyntax mockBehaviorSyntax, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        IOperation? operation = editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);

        INamedTypeSymbol? mockBehaviorType = editor.SemanticModel.Compilation.GetTypeByMetadataName(WellKnownTypeNames.MoqBehavior);

        if (mockBehaviorType is null)
        {
            return document;
        }

        SyntaxNode? newNode = null;

        if (operation is IInvocationOperation invocation)
        {
            newNode = await FixInvocation(invocation, mockBehaviorSyntax, mockBehaviorType, cancellationToken).ConfigureAwait(false);
        }

        if (operation is IObjectCreationOperation creation)
        {
            newNode = await FixObjectCreation(creation, mockBehaviorSyntax, mockBehaviorType, cancellationToken).ConfigureAwait(false);
        }

        if (newNode is not null)
        {
            editor.ReplaceNode(nodeToFix, newNode);
            return editor.GetChangedDocument();
        }

        return document;
    }

    private static async Task<SyntaxNode> FixInvocation(IInvocationOperation operation, ArgumentSyntax mockBehaviorSyntax, INamedTypeSymbol mockBehaviorType, CancellationToken cancellationToken)
    {
        if (operation.Syntax is not InvocationExpressionSyntax invocationExpression)
        {
            return operation.Syntax;
        }

        // Try adding to beginning
        SyntaxAnnotation beginAnnotation = new();
        InvocationExpressionSyntax begin = invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([mockBehaviorSyntax, ..invocationExpression.ArgumentList.Arguments])))
            .WithAdditionalAnnotations(beginAnnotation);

        ExpressionStatementSyntax beginStatement = SyntaxFactory.ExpressionStatement(begin);
        if (operation.SemanticModel.TryGetSpeculativeSemanticModel(operation.Syntax.SpanStart, beginStatement, out SemanticModel? specModel1))
        {
            SyntaxNode annotatedNode = beginStatement.GetAnnotatedNodes(beginAnnotation).Single();
            SymbolInfo x = specModel1.GetSymbolInfo(annotatedNode, cancellationToken);
            if (x.Symbol is not null)
            {
                return begin; // works
            }
        }

        // Try adding to end
        SyntaxAnnotation endAnnotation = new();
        InvocationExpressionSyntax end = invocationExpression.AddArgumentListArguments(mockBehaviorSyntax).WithAdditionalAnnotations(endAnnotation);

        ExpressionStatementSyntax endStatement = SyntaxFactory.ExpressionStatement(end);
        if (operation.SemanticModel.TryGetSpeculativeSemanticModel(operation.Syntax.SpanStart, endStatement, out SemanticModel? specModel2))
        {
            SyntaxNode annotatedNode = endStatement.GetAnnotatedNodes(endAnnotation).Single();
            SymbolInfo x = specModel2.GetSymbolInfo(annotatedNode, cancellationToken);
            if (x.Symbol is not null)
            {
                return end; // works
            }
        }

        // Try replacing MockBehavior.Default
        IArgumentOperation[] arguments = operation.Descendants().OfType<IArgumentOperation>().ToArray();//.Where(ao => ao.Type?.Equals(mockBehaviorType, SymbolEqualityComparer.Default) ?? false).ToArray();

        foreach (IArgumentOperation argument in arguments)
        {
            // TODO: Can this be refactored?
            if (argument.Value is IFieldReferenceOperation fieldReferenceOperation)
            {
                ISymbol field = fieldReferenceOperation.Member;
                if (field.ContainingType.IsInstanceOf(mockBehaviorType) && field.Name.Equals(WellKnownTypeNames.Default))
                {
                    ArgumentSyntax newArgument = mockBehaviorSyntax;
                    SyntaxNode newExpression = operation.Syntax.ReplaceNode(arguments[0].Syntax, newArgument);
                    return newExpression;
                }
            }
        }

        //operation.Arguments[0];

        //operation.Syntax
        //ImmutableArray<ISymbol> symbols = operation.SemanticModel?.GetMemberGroup(operation.Syntax, cancellationToken) ?? ImmutableArray<ISymbol>.Empty;

        return operation.Syntax;

        //IsPatternExpressionSyntax newExpression = SyntaxFactory.IsPatternExpression((ExpressionSyntax)operation.Arguments[0].Value.Syntax, SyntaxFactory.ConstantPattern((ExpressionSyntax)operation.Arguments[1].Value.Syntax));
    }

    private static async Task<SyntaxNode> FixObjectCreation(IObjectCreationOperation operation, ArgumentSyntax mockBehaviorSyntax, INamedTypeSymbol mockBehaviorType, CancellationToken cancellationToken)
    {

        return operation.Syntax;
    }
}
