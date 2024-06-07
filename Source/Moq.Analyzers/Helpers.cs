using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Moq.Analyzers;

internal static class Helpers
{
    private static readonly MoqMethodDescriptor MoqSetupMethodDescriptor = new("Setup", new Regex("^Moq\\.Mock<.*>\\.Setup\\.*"));

    private static readonly MoqMethodDescriptor MoqAsMethodDescriptor = new("As", new Regex("^Moq\\.Mock\\.As<\\.*"), isGeneric: true);

    internal static bool IsMoqSetupMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
    {
        return MoqSetupMethodDescriptor.IsMoqMethod(semanticModel, method);
    }

    internal static bool IsMoqAsMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
    {
        return MoqAsMethodDescriptor.IsMoqMethod(semanticModel, method);
    }

    internal static bool IsCallbackOrReturnInvocation(SemanticModel semanticModel, InvocationExpressionSyntax callbackOrReturnsInvocation)
    {
        var callbackOrReturnsMethod = callbackOrReturnsInvocation.Expression as MemberAccessExpressionSyntax;

        Debug.Assert(callbackOrReturnsMethod != null, nameof(callbackOrReturnsMethod) + " != null");

        if (callbackOrReturnsMethod == null)
        {
            return false;
        }

        var methodName = callbackOrReturnsMethod.Name.ToString();

        // First fast check before walking semantic model
        if (!string.Equals(methodName, "Callback", StringComparison.Ordinal)
            && !string.Equals(methodName, "Returns", StringComparison.Ordinal))
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(callbackOrReturnsMethod);
        return symbolInfo.CandidateReason switch
        {
            CandidateReason.OverloadResolutionFailure => symbolInfo.CandidateSymbols.Any(IsCallbackOrReturnSymbol),
            CandidateReason.None => IsCallbackOrReturnSymbol(symbolInfo.Symbol),
            _ => false,
        };
    }

    internal static InvocationExpressionSyntax? FindSetupMethodFromCallbackInvocation(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        var invocation = expression as InvocationExpressionSyntax;
        if (invocation?.Expression is not MemberAccessExpressionSyntax method) return null;
        if (IsMoqSetupMethod(semanticModel, method)) return invocation;
        return FindSetupMethodFromCallbackInvocation(semanticModel, method.Expression);
    }

    internal static InvocationExpressionSyntax? FindMockedMethodInvocationFromSetupMethod(InvocationExpressionSyntax? setupInvocation)
    {
        var setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as InvocationExpressionSyntax;
    }

    internal static ExpressionSyntax? FindMockedMemberExpressionFromSetupMethod(InvocationExpressionSyntax? setupInvocation)
    {
        var setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as ExpressionSyntax;
    }

    internal static IEnumerable<IMethodSymbol> GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(SemanticModel semanticModel, InvocationExpressionSyntax? setupMethodInvocation)
    {
        var setupLambdaArgument = setupMethodInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        var mockedMethodInvocation = setupLambdaArgument?.Body as InvocationExpressionSyntax;

        return GetAllMatchingSymbols<IMethodSymbol>(semanticModel, mockedMethodInvocation);
    }

    internal static IEnumerable<T> GetAllMatchingSymbols<T>(SemanticModel semanticModel, ExpressionSyntax? expression)
        where T : class
    {
        var matchingSymbols = new List<T>();
        if (expression != null)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression);
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
        var methodName = methodSymbol.ToString();
        return methodName.StartsWith("Moq.Language.ICallback", StringComparison.Ordinal)
               || methodName.StartsWith("Moq.Language.IReturns", StringComparison.Ordinal);
    }
}
