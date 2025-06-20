﻿using System.Diagnostics;

namespace Moq.Analyzers.Common;

/// <summary>
/// Extensions methods for <see cref="SemanticModel"/>.
/// </summary>
internal static class SemanticModelExtensions
{
    internal static InvocationExpressionSyntax? FindSetupMethodFromCallbackInvocation(
        this SemanticModel semanticModel,
        MoqKnownSymbols knownSymbols,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            InvocationExpressionSyntax? invocation = expression as InvocationExpressionSyntax;
            if (invocation?.Expression is not MemberAccessExpressionSyntax method)
            {
                return null;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(method, cancellationToken);
            if (symbolInfo.Symbol is null)
            {
                return null;
            }

            if (symbolInfo.Symbol.IsMoqSetupMethod(knownSymbols))
            {
                return invocation;
            }

            expression = method.Expression;
        }
    }

    internal static IEnumerable<IMethodSymbol> GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(this SemanticModel semanticModel, InvocationExpressionSyntax? setupMethodInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupMethodInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;

        return setupLambdaArgument?.Body is not InvocationExpressionSyntax mockedMethodInvocation
            ? []
            : semanticModel.GetAllMatchingSymbols<IMethodSymbol>(mockedMethodInvocation);
    }

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
        if (symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        INamedTypeSymbol? containingType = methodSymbol.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        string containingTypeName = containingType.ToDisplayString();

        bool isCallback = string.Equals(methodSymbol.Name, "Callback", StringComparison.Ordinal)
                          && containingTypeName.StartsWith("Moq.Language.ICallback", StringComparison.Ordinal);

        bool isReturns = string.Equals(methodSymbol.Name, "Returns", StringComparison.Ordinal)
                         && containingTypeName.StartsWith("Moq.Language.IReturns", StringComparison.Ordinal);

        return isCallback || isReturns;
    }
}
