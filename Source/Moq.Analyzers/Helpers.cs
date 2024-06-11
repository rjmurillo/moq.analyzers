using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Moq.Analyzers;

internal static class Helpers
{
    private static readonly MoqMethodDescriptorBase MoqSetupMethodDescriptor = new MoqMethodDescriptor("Setup", new Regex("^Moq\\.Mock<.*>\\.Setup\\.*"));

    private static readonly MoqMethodDescriptorBase MoqAsMethodDescriptor = new MoqMethodDescriptor("As", new Regex("^Moq\\.Mock\\.As<\\.*"), isGeneric: true);

    internal static bool IsMoqSetupMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
    {
        // TODO: Plumb cancellationToken through
        CancellationToken cancellationToken = default;
        return MoqSetupMethodDescriptor.IsMatch(semanticModel, method, cancellationToken);
    }

    internal static bool IsMoqAsMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
    {
        // TODO: Plumb cancellationToken through
        CancellationToken cancellationToken = default;
        return MoqAsMethodDescriptor.IsMatch(semanticModel, method, cancellationToken);
    }

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

    internal static InvocationExpressionSyntax? FindSetupMethodFromCallbackInvocation(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        InvocationExpressionSyntax? invocation = expression as InvocationExpressionSyntax;
        if (invocation?.Expression is not MemberAccessExpressionSyntax method) return null;
        if (IsMoqSetupMethod(semanticModel, method)) return invocation;
        return FindSetupMethodFromCallbackInvocation(semanticModel, method.Expression);
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

    internal static IEnumerable<T> GetAllMatchingSymbols<T>(SemanticModel semanticModel, ExpressionSyntax? expression)
        where T : class
    {
        List<T>? matchingSymbols = new List<T>();
        if (expression != null)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);
            if (symbolInfo is { CandidateReason: CandidateReason.None, Symbol: T })
            {
                matchingSymbols.Add(symbolInfo.Symbol as T);
            }
            else if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                matchingSymbols.AddRange(symbolInfo.CandidateSymbols.OfType<T>());
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
