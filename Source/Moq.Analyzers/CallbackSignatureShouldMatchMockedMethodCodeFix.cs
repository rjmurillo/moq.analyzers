using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers;

/// <summary>
/// Fixes <see cref="CallbackSignatureShouldMatchMockedMethodAnalyzer"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallbackSignatureShouldMatchMockedMethodCodeFix))]
[Shared]
public class CallbackSignatureShouldMatchMockedMethodCodeFix : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return ImmutableArray.Create(CallbackSignatureShouldMatchMockedMethodAnalyzer.RuleId); }
    }

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return;
        }

        Diagnostic? diagnostic = context.Diagnostics.First();
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
                title: "Fix Moq callback signature",
                createChangedDocument: c => FixCallbackSignatureAsync(root, context.Document, badArgumentListSyntax, c),
                equivalenceKey: "Fix Moq callback signature"),
            diagnostic);
    }

    private async Task<Document> FixCallbackSignatureAsync(SyntaxNode root, Document document, ParameterListSyntax? oldParameters, CancellationToken cancellationToken)
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        Debug.Assert(semanticModel != null, nameof(semanticModel) + " != null");

#pragma warning disable S2583 // Conditionally executed code should be reachable
        if (semanticModel == null)
        {
            return document;
        }
#pragma warning restore S2583 // Conditionally executed code should be reachable

        if (oldParameters?.Parent?.Parent?.Parent?.Parent is not InvocationExpressionSyntax callbackInvocation)
        {
            return document;
        }

        InvocationExpressionSyntax? setupMethodInvocation = Helpers.FindSetupMethodFromCallbackInvocation(semanticModel, callbackInvocation, cancellationToken);
        Debug.Assert(setupMethodInvocation != null, nameof(setupMethodInvocation) + " != null");
        IMethodSymbol[] matchingMockedMethods = Helpers.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(semanticModel, setupMethodInvocation).ToArray();

        if (matchingMockedMethods.Length != 1)
        {
            return document;
        }

        ParameterListSyntax? newParameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(matchingMockedMethods[0].Parameters.Select(
            p =>
            {
                TypeSyntax? type = SyntaxFactory.ParseTypeName(p.Type.ToMinimalDisplayString(semanticModel, oldParameters.SpanStart));
                return SyntaxFactory.Parameter(default, SyntaxFactory.TokenList(), type, SyntaxFactory.Identifier(p.Name), null);
            })));

        SyntaxNode? newRoot = root.ReplaceNode(oldParameters, newParameters);
        return document.WithSyntaxRoot(newRoot);
    }
}
