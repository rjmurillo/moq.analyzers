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
    private const string FixTitle = "Fix Moq callback signature";

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
                    FixTitle,
                    cancellationToken => FixParenthesizedCallbackSignatureAsync(root, context.Document, badArgumentListSyntax, cancellationToken),
                    FixTitle),
                diagnostic);
            return;
        }

        // SimpleLambdaExpressionSyntax nodes never contain a ParameterListSyntax,
        // so the ancestor search above intentionally falls through to this path.
        SimpleLambdaExpressionSyntax? simpleLambda = node?
                                        .AncestorsAndSelf()
                                        .OfType<SimpleLambdaExpressionSyntax>()
                                        .FirstOrDefault();

        if (simpleLambda is not null && !IsInsideDelegateConstructor(simpleLambda))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    FixTitle,
                    cancellationToken => FixSimpleLambdaCallbackSignatureAsync(root, context.Document, simpleLambda, cancellationToken),
                    FixTitle),
                diagnostic);
        }
    }

    private static async Task<Document> FixParenthesizedCallbackSignatureAsync(SyntaxNode root, Document document, ParameterListSyntax oldParameters, CancellationToken cancellationToken)
    {
        if (IsInsideDelegateConstructor(oldParameters))
        {
            return document;
        }

        ParameterListSyntax? newParameters = await ResolveNewParameterListAsync(document, oldParameters, oldParameters.SpanStart, oldParameters.Parameters, cancellationToken).ConfigureAwait(false);
        if (newParameters is null)
        {
            return document;
        }

        SyntaxNode newRoot = root.ReplaceNode(oldParameters, newParameters);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> FixSimpleLambdaCallbackSignatureAsync(SyntaxNode root, Document document, SimpleLambdaExpressionSyntax simpleLambda, CancellationToken cancellationToken)
    {
        // No delegate-constructor guard needed here; RegisterCodeFixesAsync
        // already filters out simple lambdas inside delegate constructors.
        SeparatedSyntaxList<ParameterSyntax> originalParams = SyntaxFactory.SingletonSeparatedList(simpleLambda.Parameter);
        ParameterListSyntax? newParameters = await ResolveNewParameterListAsync(document, simpleLambda, simpleLambda.SpanStart, originalParams, cancellationToken).ConfigureAwait(false);
        if (newParameters is null)
        {
            return document;
        }

        ParenthesizedLambdaExpressionSyntax parenthesizedLambda = SyntaxFactory.ParenthesizedLambdaExpression(
            simpleLambda.AsyncKeyword,
            newParameters,
            simpleLambda.ArrowToken,
            simpleLambda.Block,
            simpleLambda.ExpressionBody)
            .WithTriviaFrom(simpleLambda);

        SyntaxNode newRoot = root.ReplaceNode(simpleLambda, parenthesizedLambda);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<ParameterListSyntax?> ResolveNewParameterListAsync(Document document, SyntaxNode lambdaNode, int position, SeparatedSyntaxList<ParameterSyntax> originalParameters, CancellationToken cancellationToken)
    {
        SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return null;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);

        InvocationExpressionSyntax? callbackInvocation = lambdaNode
            .Ancestors()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (callbackInvocation is null)
        {
            return null;
        }

        IMethodSymbol? mockedMethod = FindSingleMockedMethod(semanticModel, knownSymbols, callbackInvocation, cancellationToken);
        if (mockedMethod is null)
        {
            return null;
        }

        return BuildParameterList(semanticModel, mockedMethod, position, originalParameters);
    }

    private static bool IsInsideDelegateConstructor(SyntaxNode node)
    {
        LambdaExpressionSyntax? lambda = node.FirstAncestorOrSelf<LambdaExpressionSyntax>();
        return lambda?.Parent is ArgumentSyntax
        {
            Parent: ArgumentListSyntax
            {
                Parent: ObjectCreationExpressionSyntax
            }
        };
    }

    private static IMethodSymbol? FindSingleMockedMethod(SemanticModel semanticModel, MoqKnownSymbols knownSymbols, InvocationExpressionSyntax callbackInvocation, CancellationToken cancellationToken)
    {
        InvocationExpressionSyntax? setupMethodInvocation = semanticModel.FindSetupMethodFromCallbackInvocation(knownSymbols, callbackInvocation, cancellationToken);
        if (setupMethodInvocation is null)
        {
            return null;
        }

        // Short-circuit: we only need to know if there is exactly one match.
        IMethodSymbol[] matchingMockedMethods = semanticModel.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(setupMethodInvocation).Take(2).ToArray();

        if (matchingMockedMethods.Length != 1)
        {
            return null;
        }

        return matchingMockedMethods[0];
    }

    private static ParameterListSyntax BuildParameterList(SemanticModel semanticModel, IMethodSymbol mockedMethod, int position, SeparatedSyntaxList<ParameterSyntax> originalParameters)
    {
        ImmutableArray<IParameterSymbol> parameters = mockedMethod.Parameters;
        ParameterSyntax[] result = new ParameterSyntax[parameters.Length];

        for (int index = 0; index < parameters.Length; index++)
        {
            IParameterSymbol parameterSymbol = parameters[index];
            TypeSyntax type = SyntaxFactory.ParseTypeName(parameterSymbol.Type.ToMinimalDisplayString(semanticModel, position));
            SyntaxTokenList modifiers = GetParameterModifiers(parameterSymbol.RefKind);

            string name = index < originalParameters.Count
                ? originalParameters[index].Identifier.ValueText
                : parameterSymbol.Name;

            SyntaxToken identifier = SyntaxFactory.Identifier(name);

            result[index] = SyntaxFactory.Parameter(default, modifiers, type, identifier, null);
        }

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(result));
    }

    private static SyntaxTokenList GetParameterModifiers(RefKind refKind)
    {
        return refKind switch
        {
            RefKind.Ref => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
            RefKind.Out => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
            RefKind.In => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword)),
            _ => SyntaxFactory.TokenList(),
        };
    }
}
