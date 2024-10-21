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

        context.RegisterCodeFix(CodeAction.Create("Set MockBehavior (Loose)", ct => AddMockBehaviorAsync(context.Document, nodeToFix, ct), equivalenceKey: "Set MockBehavior (Loose)"), context.Diagnostics);
        context.RegisterCodeFix(CodeAction.Create("Set MockBehavior (Strict)", ct => AddMockBehaviorAsync(context.Document, nodeToFix, ct), equivalenceKey: "Set MockBehavior (Strict)"), context.Diagnostics);
    }

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    private static async Task<Document> AddMockBehaviorAsync(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        IOperation? operation = editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);

        SyntaxNode? newNode = null;

        if (operation is IInvocationOperation invocation)
        {
            newNode = await FixInvocation(invocation, cancellationToken).ConfigureAwait(false);
        }

        if (operation is IObjectCreationOperation creation)
        {
            newNode = await FixObjectCreation(creation, cancellationToken).ConfigureAwait(false);
        }

        if (newNode is not null)
        {
            editor.ReplaceNode(nodeToFix, newNode);
            return editor.GetChangedDocument();
        }

        return document;
    }

    private static async Task<SyntaxNode> FixInvocation(IInvocationOperation operation, CancellationToken cancellationToken)
    {
        if (operation.Syntax is not InvocationExpressionSyntax invocationExpression)
        {
            return operation.Syntax;
        }

        ArgumentSyntax loose = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("MockBehavior"), SyntaxFactory.IdentifierName("Loose")));

        SyntaxAnnotation beginAnnotation = new SyntaxAnnotation();
        InvocationExpressionSyntax begin = invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([loose, ..invocationExpression.ArgumentList.Arguments])))
            .WithAdditionalAnnotations(beginAnnotation);

        SyntaxAnnotation endAnnotation = new SyntaxAnnotation();
        InvocationExpressionSyntax end = invocationExpression.AddArgumentListArguments(loose).WithAdditionalAnnotations(endAnnotation);

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

        //operation.Descendants().OfType<IArgumentOperation>().Where(ao => ao.Type.Equals(WellKnownTypeNames.MockBehavior)).Select(x => x.Syntax).ToArray();

        //operation.Arguments[0];

        //operation.Syntax
        //ImmutableArray<ISymbol> symbols = operation.SemanticModel?.GetMemberGroup(operation.Syntax, cancellationToken) ?? ImmutableArray<ISymbol>.Empty;

        return operation.Syntax;

        //IsPatternExpressionSyntax newExpression = SyntaxFactory.IsPatternExpression((ExpressionSyntax)operation.Arguments[0].Value.Syntax, SyntaxFactory.ConstantPattern((ExpressionSyntax)operation.Arguments[1].Value.Syntax));
    }

    private static async Task<SyntaxNode> FixObjectCreation(IObjectCreationOperation operation, CancellationToken cancellationToken)
    {

        return operation.Syntax;
    }
}
