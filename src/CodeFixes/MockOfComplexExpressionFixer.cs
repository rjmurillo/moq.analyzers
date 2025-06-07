using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.MockOfComplexExpression"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MockOfComplexExpressionFixer))]
[Shared]
public class MockOfComplexExpressionFixer : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.MockOfComplexExpression);

    /// <inheritdoc />
    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        SyntaxNode? nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (nodeToFix is not InvocationExpressionSyntax invocationExpression)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Replace with Mock<T>",
                cancellationToken => ReplaceWithMockAsync(context.Document, root, invocationExpression),
                "Replace with Mock<T>"),
            diagnostic);
    }

    private static Task<Document> ReplaceWithMockAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocationExpression)
    {
        // Extract the type argument from Mock.Of<T>()
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name is not GenericNameSyntax genericName ||
            genericName.TypeArgumentList.Arguments.Count != 1)
        {
            return Task.FromResult(document);
        }

        TypeSyntax typeArgument = genericName.TypeArgumentList.Arguments[0];

        // Create new Mock<T>() expression
        ObjectCreationExpressionSyntax newExpression = SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.GenericName(SyntaxFactory.Identifier("Mock"))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(typeArgument))))
            .WithArgumentList(SyntaxFactory.ArgumentList());

        SyntaxNode newRoot = root.ReplaceNode(invocationExpression, newExpression);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
