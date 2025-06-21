using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Fixes Moq.Verify calls on non-overridable members by making them virtual.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VerifyOverridableMembersFixer))]
[Shared]
public sealed class VerifyOverridableMembersFixer : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.VerifyOnlyUsedForOverridableMembers);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        SyntaxNode? invocationNode = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
        if (invocationNode is not InvocationExpressionSyntax)
        {
            invocationNode = invocationNode.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        }

        if (invocationNode == null)
        {
            return;
        }

        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        IOperation? operation = semanticModel.GetOperation(invocationNode, context.CancellationToken);
        if (operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        ISymbol? mockedMemberSymbol = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocationOperation);
        if (mockedMemberSymbol == null)
        {
            return;
        }

        // Check if we should offer a fix.
        if (!(mockedMemberSymbol is IPropertySymbol or IMethodSymbol) || mockedMemberSymbol.IsOverridable() || mockedMemberSymbol.IsSealed)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Make member virtual",
                cancellationToken => MakeMemberVirtualAsync(context.Document, mockedMemberSymbol, cancellationToken),
                nameof(VerifyOverridableMembersFixer)),
            diagnostic);
    }

    private static async Task<Solution> MakeMemberVirtualAsync(
        Document document,
        ISymbol memberSymbol,
        CancellationToken cancellationToken)
    {
        Solution solution = document.Project.Solution;

        SyntaxReference? declarationSyntaxReference = memberSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (declarationSyntaxReference == null)
        {
            return solution;
        }

        Document? memberDocument = solution.GetDocument(declarationSyntaxReference.SyntaxTree);
        if (memberDocument == null)
        {
            return solution;
        }

        SyntaxNode memberDeclarationNode = await declarationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

        DocumentEditor documentEditor = await DocumentEditor.CreateAsync(memberDocument, cancellationToken).ConfigureAwait(false);
        SyntaxGenerator syntaxGenerator = SyntaxGenerator.GetGenerator(memberDocument);

        DeclarationModifiers modifiers = syntaxGenerator.GetModifiers(memberDeclarationNode);
        if (modifiers.IsVirtual)
        {
            return solution;
        }

        DeclarationModifiers newModifiers = modifiers.WithIsVirtual(true);

        SyntaxNode newDeclaration = syntaxGenerator.WithModifiers(memberDeclarationNode, newModifiers);

        documentEditor.ReplaceNode(memberDeclarationNode, newDeclaration);

        return documentEditor.GetChangedDocument().Project.Solution;
    }
}
