using System.Diagnostics;

namespace Moq.Analyzers;

/// <summary>
/// Extensions methods for <see cref="SemanticModel"/>.
/// </summary>
internal static class SemanticModelExtensions
{
    private static readonly MoqMethodDescriptorBase MoqSetupMethodDescriptor = new MoqSetupMethodDescriptor();

    internal static InvocationExpressionSyntax? FindSetupMethodFromCallbackInvocation(this SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        InvocationExpressionSyntax? invocation = expression as InvocationExpressionSyntax;
        if (invocation?.Expression is not MemberAccessExpressionSyntax method) return null;
        if (IsMoqSetupMethod(semanticModel, method, cancellationToken)) return invocation;
        return FindSetupMethodFromCallbackInvocation(semanticModel, method.Expression, cancellationToken);
    }

    internal static IEnumerable<IMethodSymbol> GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(this SemanticModel semanticModel, InvocationExpressionSyntax? setupMethodInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupMethodInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;

        return setupLambdaArgument?.Body is not InvocationExpressionSyntax mockedMethodInvocation
            ? []
            : semanticModel.GetAllMatchingSymbols<IMethodSymbol>(mockedMethodInvocation);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    internal static bool IsCallbackOrReturnInvocation(this SemanticModel semanticModel, InvocationExpressionSyntax callbackOrReturnsInvocation)
    {
        MemberAccessExpressionSyntax? callbackOrReturnsMethod = callbackOrReturnsInvocation.Expression as MemberAccessExpressionSyntax;

        if (callbackOrReturnsMethod == null)
        {
            return false;
        }

        string methodName = callbackOrReturnsMethod.Name.ToString();

        // First fast check before walking semantic model
        if (!string.Equals(methodName, "Callback", StringComparison.Ordinal)
            && !string.Equals(methodName, "Returns", StringComparison.Ordinal))
        {
            return false;
        }

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(callbackOrReturnsMethod);
        return symbolInfo.CandidateReason switch
        {
            CandidateReason.OverloadResolutionFailure => symbolInfo.CandidateSymbols.Any(IsCallbackOrReturnSymbol),
            CandidateReason.None => IsCallbackOrReturnSymbol(symbolInfo.Symbol),
            _ => false,
        };
    }

    internal static bool IsMoqSetupMethod(this SemanticModel semanticModel, MemberAccessExpressionSyntax method, CancellationToken cancellationToken)
    {
        return MoqSetupMethodDescriptor.IsMatch(semanticModel, method, cancellationToken);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    private static List<T> GetAllMatchingSymbols<T>(this SemanticModel semanticModel, ExpressionSyntax expression)
        where T : class
    {
        List<T> matchingSymbols = new();

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);
        switch (symbolInfo)
        {
            case { CandidateReason: CandidateReason.None, Symbol: T }:
                {
                    T? value = symbolInfo.Symbol as T;
                    Debug.Assert(value != null, "Value should not be null.");

#pragma warning disable S2589 // Boolean expressions should not be gratuitous
                    if (value != default(T))
                    {
                        matchingSymbols.Add(value);
                    }
#pragma warning restore S2589 // Boolean expressions should not be gratuitous
                    break;
                }

            default:
                {
                    if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
                    {
                        matchingSymbols.AddRange(symbolInfo.CandidateSymbols.OfType<T>());
                    }
                    else
                    {
                        return matchingSymbols;
                    }

                    break;
                }
        }

        return matchingSymbols;
    }

    private static bool IsCallbackOrReturnSymbol(ISymbol? symbol)
    {
        // TODO: Check what is the best way to do such checks
        if (symbol is not IMethodSymbol methodSymbol) return false;
        string? methodName = methodSymbol.ToString();
        return methodName.StartsWith("Moq.Language.ICallback", StringComparison.Ordinal)
               || methodName.StartsWith("Moq.Language.IReturns", StringComparison.Ordinal);
    }
}
