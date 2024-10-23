﻿using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for SetExplicitMockBehaviorAnalyzer (Moq1400).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SetExplicitMockBehaviorFixer))]
[Shared]
public class SetExplicitMockBehaviorFixer : CodeFixProvider
{
    private static readonly ArgumentSyntax LooseSyntax = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(WellKnownTypeNames.MockBehavior), SyntaxFactory.IdentifierName(WellKnownTypeNames.Loose)));
    private static readonly ArgumentSyntax StrictSyntax = SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(WellKnownTypeNames.MockBehavior), SyntaxFactory.IdentifierName(WellKnownTypeNames.Strict)));

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.SetExplicitMockBehavior);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SyntaxNode? nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);

        if (nodeToFix is null)
        {
            return;
        }

        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Loose)", context.Document, nodeToFix, LooseSyntax), context.Diagnostics);
        context.RegisterCodeFix(new SetExplicitMockBehaviorCodeAction("Set MockBehavior (Strict)", context.Document, nodeToFix, StrictSyntax), context.Diagnostics);
    }

    private sealed class SetExplicitMockBehaviorCodeAction : CodeAction
    {
        private readonly Document _document;
        private readonly SyntaxNode _nodeToFix;
        private readonly ArgumentSyntax _mockBehaviorSyntax;

        public SetExplicitMockBehaviorCodeAction(string title, Document document, SyntaxNode nodeToFix, ArgumentSyntax mockBehaviorSyntax)
        {
            Title = title;
            _document = document;
            _nodeToFix = nodeToFix;
            _mockBehaviorSyntax = mockBehaviorSyntax;
        }

        public override string Title { get; }

        public override string? EquivalenceKey => Title;

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken) => AddMockBehaviorAsync(_document, _nodeToFix, _mockBehaviorSyntax, cancellationToken);

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
                newNode = FixInvocation(invocation, mockBehaviorSyntax, mockBehaviorType, cancellationToken);
            }

            if (operation is IObjectCreationOperation creation)
            {
                newNode = FixObjectCreation(creation, mockBehaviorSyntax, mockBehaviorType, cancellationToken);
            }

            if (newNode is not null)
            {
                editor.ReplaceNode(nodeToFix, newNode);
                return editor.GetChangedDocument();
            }

            return document;
        }

        private static SyntaxNode FixInvocation(IInvocationOperation operation, ArgumentSyntax mockBehaviorSyntax, INamedTypeSymbol mockBehaviorType, CancellationToken cancellationToken)
        {
            if (operation.Syntax is not InvocationExpressionSyntax invocationExpression)
            {
                return operation.Syntax;
            }

            // Try replacing MockBehavior.Default
            IArgumentOperation[] arguments = operation.Descendants().OfType<IArgumentOperation>().ToArray();

            foreach (IArgumentOperation argument in arguments)
            {
                // TODO: Can this be refactored? IMemberReferenceOperation
                if (argument.Value is IFieldReferenceOperation fieldReferenceOperation)
                {
                    ISymbol field = fieldReferenceOperation.Member;
                    if (field.ContainingType.IsInstanceOf(mockBehaviorType) && field.Name.Equals(WellKnownTypeNames.Default, StringComparison.Ordinal))
                    {
                        ArgumentSyntax newArgument = mockBehaviorSyntax;
                        SyntaxNode newExpression = operation.Syntax.ReplaceNode(argument.Syntax, newArgument);
                        return newExpression;
                    }
                }
            }

            // Try adding to beginning
            SyntaxAnnotation beginAnnotation = new();
            InvocationExpressionSyntax begin = invocationExpression.PrependArgumentListArguments(mockBehaviorSyntax).WithAdditionalAnnotations(beginAnnotation);

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

            return operation.Syntax;
        }

        private static SyntaxNode FixObjectCreation(IObjectCreationOperation operation, ArgumentSyntax mockBehaviorSyntax, INamedTypeSymbol mockBehaviorType, CancellationToken cancellationToken)
        {
            if (operation.Syntax is not ObjectCreationExpressionSyntax creationExpression)
            {
                return operation.Syntax;
            }

            // Try replacing MockBehavior.Default
            IArgumentOperation[] arguments = operation.Descendants().OfType<IArgumentOperation>().ToArray();

            foreach (IArgumentOperation argument in arguments)
            {
                // TODO: Can this be refactored? IMemberReferenceOperation
                if (argument.Value is IFieldReferenceOperation fieldReferenceOperation)
                {
                    ISymbol field = fieldReferenceOperation.Member;
                    if (field.ContainingType.IsInstanceOf(mockBehaviorType) && field.Name.Equals(WellKnownTypeNames.Default, StringComparison.Ordinal))
                    {
                        ArgumentSyntax newArgument = mockBehaviorSyntax;
                        SyntaxNode newExpression = operation.Syntax.ReplaceNode(argument.Syntax, newArgument);
                        return newExpression;
                    }
                }
            }

            // Try adding to beginning
            SyntaxAnnotation beginAnnotation = new();
            ObjectCreationExpressionSyntax begin = creationExpression.PrependArgumentListArguments(mockBehaviorSyntax).WithAdditionalAnnotations(beginAnnotation);

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
            ObjectCreationExpressionSyntax end = creationExpression.AddArgumentListArguments(mockBehaviorSyntax).WithAdditionalAnnotations(endAnnotation);

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

            return operation.Syntax;
        }
    }
}
