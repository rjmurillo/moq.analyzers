using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for <see cref="DiagnosticIds.MockRepositoryVerifyMissing"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MockRepositoryVerifyMissingFixer))]
[Shared]
public class MockRepositoryVerifyMissingFixer : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.MockRepositoryVerifyMissing);

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

        if (nodeToFix is not VariableDeclaratorSyntax variableDeclarator)
        {
            return;
        }

        // Find the repository variable name
        string repositoryVariableName = variableDeclarator.Identifier.ValueText;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add repository.Verify() call",
                cancellationToken => AddRepositoryVerifyCallAsync(context.Document, root, variableDeclarator, repositoryVariableName),
                "Add repository.Verify() call"),
            diagnostic);
    }

    private static Task<Document> AddRepositoryVerifyCallAsync(
        Document document,
        SyntaxNode root,
        VariableDeclaratorSyntax variableDeclarator,
        string repositoryVariableName)
    {
        // Find the containing method or constructor
        MethodDeclarationSyntax? containingMethod = variableDeclarator.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        ConstructorDeclarationSyntax? containingConstructor = variableDeclarator.Ancestors().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

        BlockSyntax? targetBlock = containingMethod?.Body ?? containingConstructor?.Body;

        if (targetBlock == null)
        {
            return Task.FromResult(document);
        }

        // Create the repository.Verify() statement with proper indentation
        ExpressionStatementSyntax verifyStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(repositoryVariableName),
                    SyntaxFactory.IdentifierName("Verify"))))
            .WithLeadingTrivia(targetBlock.Statements.LastOrDefault()?.GetLeadingTrivia() ?? SyntaxFactory.TriviaList());

        // Add the Verify() call at the end of the method/constructor body
        BlockSyntax newBlock = targetBlock.AddStatements(verifyStatement);

        SyntaxNode newRoot = root.ReplaceNode(targetBlock, newBlock);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
