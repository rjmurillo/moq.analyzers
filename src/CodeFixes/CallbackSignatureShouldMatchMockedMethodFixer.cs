using System.Composition;
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

        SyntaxNode? node = root.FindToken(diagnosticSpan.Start).Parent;

        // Try parenthesized lambda path first (diagnostic on ParameterListSyntax).
        ParameterListSyntax? badArgumentListSyntax = node?
                                        .AncestorsAndSelf()
                                        .OfType<ParameterListSyntax>()
                                        .FirstOrDefault();

        if (badArgumentListSyntax is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Fix Moq callback signature",
                    cancellationToken => FixParenthesizedCallbackSignatureAsync(root, context.Document, badArgumentListSyntax, cancellationToken),
                    "Fix Moq callback signature"),
                diagnostic);
            return;
        }

        // Try simple lambda path (diagnostic on ParameterSyntax within SimpleLambdaExpressionSyntax).
        SimpleLambdaExpressionSyntax? simpleLambda = node?
                                        .AncestorsAndSelf()
                                        .OfType<SimpleLambdaExpressionSyntax>()
                                        .FirstOrDefault();

        if (simpleLambda is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Fix Moq callback signature",
                    cancellationToken => FixSimpleLambdaCallbackSignatureAsync(root, context.Document, simpleLambda, cancellationToken),
                    "Fix Moq callback signature"),
                diagnostic);
        }
    }

    private static async Task<Document> FixParenthesizedCallbackSignatureAsync(SyntaxNode root, Document document, ParameterListSyntax oldParameters, CancellationToken cancellationToken)
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null)
        {
            return document;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);

        if (oldParameters.Parent?.Parent?.Parent?.Parent is not InvocationExpressionSyntax callbackInvocation)
        {
            return document;
        }

        IMethodSymbol? mockedMethod = FindSingleMockedMethod(semanticModel, knownSymbols, callbackInvocation, cancellationToken);
        if (mockedMethod is null)
        {
            return document;
        }

        ParameterListSyntax newParameters = BuildParameterList(semanticModel, mockedMethod, oldParameters.SpanStart);

        SyntaxNode newRoot = root.ReplaceNode(oldParameters, newParameters);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> FixSimpleLambdaCallbackSignatureAsync(SyntaxNode root, Document document, SimpleLambdaExpressionSyntax simpleLambda, CancellationToken cancellationToken)
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null)
        {
            return document;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);

        InvocationExpressionSyntax? callbackInvocation = simpleLambda
            .Ancestors()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (callbackInvocation is null)
        {
            return document;
        }

        IMethodSymbol? mockedMethod = FindSingleMockedMethod(semanticModel, knownSymbols, callbackInvocation, cancellationToken);
        if (mockedMethod is null)
        {
            return document;
        }

        ParameterListSyntax newParameters = BuildParameterList(semanticModel, mockedMethod, simpleLambda.SpanStart);

        ParenthesizedLambdaExpressionSyntax parenthesizedLambda = SyntaxFactory.ParenthesizedLambdaExpression(
            simpleLambda.AsyncKeyword,
            newParameters,
            simpleLambda.ArrowToken,
            simpleLambda.Block,
            simpleLambda.ExpressionBody);

        SyntaxNode newRoot = root.ReplaceNode(simpleLambda, parenthesizedLambda);
        return document.WithSyntaxRoot(newRoot);
    }

    private static IMethodSymbol? FindSingleMockedMethod(SemanticModel semanticModel, MoqKnownSymbols knownSymbols, InvocationExpressionSyntax callbackInvocation, CancellationToken cancellationToken)
    {
        InvocationExpressionSyntax? setupMethodInvocation = semanticModel.FindSetupMethodFromCallbackInvocation(knownSymbols, callbackInvocation, cancellationToken);
        if (setupMethodInvocation is null)
        {
            return null;
        }

        IMethodSymbol[] matchingMockedMethods = semanticModel.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(setupMethodInvocation).ToArray();

        if (matchingMockedMethods.Length != 1)
        {
            return null;
        }

        return matchingMockedMethods[0];
    }

    private static ParameterListSyntax BuildParameterList(SemanticModel semanticModel, IMethodSymbol mockedMethod, int position)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(mockedMethod.Parameters.Select(
            parameterSymbol =>
            {
                TypeSyntax type = SyntaxFactory.ParseTypeName(parameterSymbol.Type.ToMinimalDisplayString(semanticModel, position));
                SyntaxTokenList modifiers = GetParameterModifiers(parameterSymbol.RefKind);
                return SyntaxFactory.Parameter(default, modifiers, type, SyntaxFactory.Identifier(parameterSymbol.Name), null);
            })));
    }

    private static SyntaxTokenList GetParameterModifiers(RefKind refKind)
    {
        return refKind switch
        {
            RefKind.Ref => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
            RefKind.Out => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
            RefKind.In => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword)),
            RefKind.None => SyntaxFactory.TokenList(),
            _ => SyntaxFactory.TokenList(),
        };
    }
}
