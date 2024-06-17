using System.Diagnostics;
using System.Linq.Expressions;

namespace Moq.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "AV1708:Type name contains term that should be avoided", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
internal static class Helpers
{
    private static readonly MoqMethodDescriptorBase MoqSetupMethodDescriptor = new MoqSetupMethodDescriptor();

    internal static bool IsMoqSetupMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method, CancellationToken cancellationToken)
    {
        return MoqSetupMethodDescriptor.IsMatch(semanticModel, method, cancellationToken);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    internal static bool IsCallbackOrReturnInvocation(SemanticModel semanticModel, InvocationExpressionSyntax callbackOrReturnsInvocation)
    {
        MemberAccessExpressionSyntax? callbackOrReturnsMethod = callbackOrReturnsInvocation.Expression as MemberAccessExpressionSyntax;

        Debug.Assert(callbackOrReturnsMethod != null, nameof(callbackOrReturnsMethod) + " != null");

#pragma warning disable S2583 // Conditionally executed code should be reachable
        if (callbackOrReturnsMethod == null)
        {
            return false;
        }
#pragma warning restore S2583 // Conditionally executed code should be reachable

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

    internal static InvocationExpressionSyntax? FindSetupMethodFromCallbackInvocation(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        InvocationExpressionSyntax? invocation = expression as InvocationExpressionSyntax;
        if (invocation?.Expression is not MemberAccessExpressionSyntax method) return null;
        if (IsMoqSetupMethod(semanticModel, method, cancellationToken)) return invocation;
        return FindSetupMethodFromCallbackInvocation(semanticModel, method.Expression, cancellationToken);
    }

    internal static InvocationExpressionSyntax? FindMockedMethodInvocationFromSetupMethod(InvocationExpressionSyntax? setupInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as InvocationExpressionSyntax;
    }

    internal static ExpressionSyntax? FindMockedMemberExpressionFromSetupMethod(InvocationExpressionSyntax? setupInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as ExpressionSyntax;
    }

    internal static IEnumerable<IMethodSymbol> GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(SemanticModel semanticModel, InvocationExpressionSyntax? setupMethodInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupMethodInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        InvocationExpressionSyntax? mockedMethodInvocation = setupLambdaArgument?.Body as InvocationExpressionSyntax;

        return mockedMethodInvocation == null
            ? []
            : GetAllMatchingSymbols<IMethodSymbol>(semanticModel, mockedMethodInvocation);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    internal static IEnumerable<T> GetAllMatchingSymbols<T>(SemanticModel semanticModel, ExpressionSyntax expression)
        where T : class
    {
        List<T> matchingSymbols = new();

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);
        if (symbolInfo is { CandidateReason: CandidateReason.None, Symbol: T })
        {
            T? value = symbolInfo.Symbol as T;
            Debug.Assert(value != null, "Value should not be null.");

#pragma warning disable S2589 // Boolean expressions should not be gratuitous
            if (value != default(T))
            {
                matchingSymbols.Add(value);
            }
#pragma warning restore S2589 // Boolean expressions should not be gratuitous
        }
        else if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
        {
            matchingSymbols.AddRange(symbolInfo.CandidateSymbols.OfType<T>());
        }
        else
        {
            return matchingSymbols;
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
