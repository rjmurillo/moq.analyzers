using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Moq.CodeFixes;

/// <summary>
/// Fixes for CallbackSignatureShouldMatchMockedMethodAnalyzer (Moq1100).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallbackSignatureShouldMatchMockedMethodFixer))]
[Shared]
public class CallbackSignatureShouldMatchMockedMethodFixer : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.BadCallbackParameters);

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        ParameterListSyntax? badArgumentListSyntax = root.FindToken(diagnosticSpan.Start)
                                        .Parent?
                                        .AncestorsAndSelf()
                                        .OfType<ParameterListSyntax>()
                                        .First();

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                "Fix Moq callback signature",
                cancellationToken => FixCallbackSignatureAsync(root, context.Document, badArgumentListSyntax, cancellationToken),
                "Fix Moq callback signature"),
            diagnostic);
    }

    private static async Task<Document> FixCallbackSignatureAsync(SyntaxNode root, Document document, ParameterListSyntax? oldParameters, CancellationToken cancellationToken)
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null)
        {
            return document;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);

        if (oldParameters?.Parent?.Parent?.Parent?.Parent is not InvocationExpressionSyntax callbackInvocation)
        {
            return document;
        }

        InvocationExpressionSyntax? setupMethodInvocation = semanticModel.FindSetupMethodFromCallbackInvocation(knownSymbols, callbackInvocation, cancellationToken);
        Debug.Assert(setupMethodInvocation != null, nameof(setupMethodInvocation) + " != null");
        IMethodSymbol[] matchingMockedMethods = semanticModel.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(setupMethodInvocation).ToArray();

        if (matchingMockedMethods.Length != 1)
        {
            return document;
        }

        ParameterListSyntax newParameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(matchingMockedMethods[0].Parameters.Select(
            parameterSymbol =>
            {
                TypeSyntax type = SyntaxFactory.ParseTypeName(parameterSymbol.Type.ToMinimalDisplayString(semanticModel, oldParameters.SpanStart));
                return SyntaxFactory.Parameter(default, SyntaxFactory.TokenList(), type, SyntaxFactory.Identifier(parameterSymbol.Name), null);
            })));

        SyntaxNode newRoot = root.ReplaceNode(oldParameters, newParameters);
        return document.WithSyntaxRoot(newRoot);
    }
}
