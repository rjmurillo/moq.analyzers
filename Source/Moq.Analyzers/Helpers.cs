using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Moq.Analyzers;

internal static class Helpers
{
    private static MoqMethodDescriptor moqSetupMethodDescriptor = new MoqMethodDescriptor("Setup", new Regex("^Moq\\.Mock<.*>\\.Setup\\.*"));

    private static MoqMethodDescriptor moqAsMethodDescriptor = new MoqMethodDescriptor("As", new Regex("^Moq\\.Mock\\.As<\\.*"), isGeneric: true);

    internal static bool IsMoqSetupMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
    {
        return moqSetupMethodDescriptor.IsMoqMethod(semanticModel, method);
    }

    internal static bool IsMoqAsMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
    {
        return moqAsMethodDescriptor.IsMoqMethod(semanticModel, method);
    }

    internal static bool IsCallbackOrReturnInvocation(SemanticModel semanticModel, InvocationExpressionSyntax callbackOrReturnsInvocation)
    {
        var callbackOrReturnsMethod = callbackOrReturnsInvocation.Expression as MemberAccessExpressionSyntax;
        var methodName = callbackOrReturnsMethod?.Name.ToString();

        // First fast check before walking semantic model
        if (methodName != "Callback" && methodName != "Returns")
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(callbackOrReturnsMethod);
        if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
        {
            return symbolInfo.CandidateSymbols.Any(s => IsCallbackOrReturnSymbol(s));
        }
        else if (symbolInfo.CandidateReason == CandidateReason.None)
        {
            return IsCallbackOrReturnSymbol(symbolInfo.Symbol);
        }

        return false;
    }

    internal static InvocationExpressionSyntax FindSetupMethodFromCallbackInvocation(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        var invocation = expression as InvocationExpressionSyntax;
        var method = invocation?.Expression as MemberAccessExpressionSyntax;
        if (method == null) return null;
        if (IsMoqSetupMethod(semanticModel, method)) return invocation;
        return FindSetupMethodFromCallbackInvocation(semanticModel, method.Expression);
    }

    internal static InvocationExpressionSyntax FindMockedMethodInvocationFromSetupMethod(InvocationExpressionSyntax setupInvocation)
    {
        var setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0]?.Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as InvocationExpressionSyntax;
    }

    internal static ExpressionSyntax FindMockedMemberExpressionFromSetupMethod(InvocationExpressionSyntax setupInvocation)
    {
        var setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0]?.Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as ExpressionSyntax;
    }

    internal static IEnumerable<IMethodSymbol> GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(SemanticModel semanticModel, InvocationExpressionSyntax setupMethodInvocation)
    {
        var setupLambdaArgument = setupMethodInvocation?.ArgumentList.Arguments[0]?.Expression as LambdaExpressionSyntax;
        var mockedMethodInvocation = setupLambdaArgument?.Body as InvocationExpressionSyntax;

        return GetAllMatchingSymbols<IMethodSymbol>(semanticModel, mockedMethodInvocation);
    }

    internal static IEnumerable<T> GetAllMatchingSymbols<T>(SemanticModel semanticModel, ExpressionSyntax expression)
        where T : class
    {
        var matchingSymbols = new List<T>();
        if (expression != null)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            if (symbolInfo.CandidateReason == CandidateReason.None && symbolInfo.Symbol is T)
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

    private static bool IsCallbackOrReturnSymbol(ISymbol symbol)
    {
        // TODO: Check what is the best way to do such checks
        var methodSymbol = symbol as IMethodSymbol;
        if (methodSymbol == null) return false;
        var methodName = methodSymbol.ToString();
        return methodName.StartsWith("Moq.Language.ICallback") || methodName.StartsWith("Moq.Language.IReturns");
    }
}
