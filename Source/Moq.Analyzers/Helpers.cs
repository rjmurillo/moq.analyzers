namespace Moq.Analyzers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Helpers
    {
        private static readonly Regex SetupMethodNamePattern = new Regex("^Moq\\.Mock<.*>\\.Setup\\.*");

        internal static bool IsMoqSetupMethod(SemanticModel semanticModel, MemberAccessExpressionSyntax method)
        {
            var methodName = method?.Name.ToString();

            // First fast check before walking semantic model
            if (methodName != "Setup") return false;

            var symbolInfo = semanticModel.GetSymbolInfo(method);
            if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
            {
                return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any(s => SetupMethodNamePattern.IsMatch(s.ToString()));
            }
            else if (symbolInfo.CandidateReason == CandidateReason.None)
            {
                // TODO: Replace regex with something more elegant
                return symbolInfo.Symbol is IMethodSymbol && SetupMethodNamePattern.IsMatch(symbolInfo.Symbol.ToString());
            }

            return false;
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
}
