using System.Diagnostics;

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

        if (callbackOrReturnsMethod == null)
        {
            return false;
        }

        string? methodName = callbackOrReturnsMethod.Name.ToString();

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

        return GetAllMatchingSymbols<IMethodSymbol>(semanticModel, mockedMethodInvocation);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    internal static IEnumerable<T> GetAllMatchingSymbols<T>(SemanticModel semanticModel, ExpressionSyntax? expression)
        where T : class
    {
        if (expression == null)
        {
            return Enumerable.Empty<T>();
        }

        List<T> matchingSymbols = new();

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);
        if (symbolInfo is { CandidateReason: CandidateReason.None, Symbol: T })
        {
            matchingSymbols.Add(symbolInfo.Symbol as T);
        }
        else if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
        {
            matchingSymbols.AddRange(symbolInfo.CandidateSymbols.OfType<T>());
        }
        else
        {
            throw new NotSupportedException("Symbol not supported.");
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
