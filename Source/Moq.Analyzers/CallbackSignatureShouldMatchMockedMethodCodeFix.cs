using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallbackSignatureShouldMatchMockedMethodCodeFix))]
[Shared]
public class CallbackSignatureShouldMatchMockedMethodCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return ImmutableArray.Create(Diagnostics.CallbackSignatureShouldMatchMockedMethodId); }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        Diagnostic? diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        ParameterListSyntax? badArgumentListSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ParameterListSyntax>().First();

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Fix Moq callback signature",
                createChangedDocument: c => FixCallbackSignature(root, context.Document, badArgumentListSyntax, c),
                equivalenceKey: "Fix Moq callback signature"),
            diagnostic);
    }

    private async Task<Document> FixCallbackSignature(SyntaxNode root, Document document, ParameterListSyntax oldParameters, CancellationToken cancellationToken)
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        InvocationExpressionSyntax? callbackInvocation = oldParameters?.Parent?.Parent?.Parent?.Parent as InvocationExpressionSyntax;
        if (callbackInvocation != null)
        {
            InvocationExpressionSyntax setupMethodInvocation = Helpers.FindSetupMethodFromCallbackInvocation(semanticModel, callbackInvocation);
            IMethodSymbol[] matchingMockedMethods = Helpers.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(semanticModel, setupMethodInvocation).ToArray();

            if (matchingMockedMethods.Length == 1)
            {
                ParameterListSyntax? newParameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(matchingMockedMethods[0].Parameters.Select(
                    p =>
                    {
                        TypeSyntax? type = SyntaxFactory.ParseTypeName(p.Type.ToMinimalDisplayString(semanticModel, oldParameters.SpanStart));
                        return SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.TokenList(), type, SyntaxFactory.Identifier(p.Name), null);
                    })));

                SyntaxNode? newRoot = root.ReplaceNode(oldParameters, newParameters);
                return document.WithSyntaxRoot(newRoot);
            }
        }

        return document;
    }
}
