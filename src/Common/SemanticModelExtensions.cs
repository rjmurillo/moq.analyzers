using System.Diagnostics;

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

    internal static bool IsCallbackOrReturnInvocation(this SemanticModel semanticModel, InvocationExpressionSyntax callbackOrReturnsInvocation, MoqKnownSymbols knownSymbols)
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
            CandidateReason.OverloadResolutionFailure => symbolInfo.CandidateSymbols.Any(symbol => IsCallbackOrReturnSymbol(symbol, knownSymbols)),
            CandidateReason.None => IsCallbackOrReturnSymbol(symbolInfo.Symbol, knownSymbols),
            _ => false,
        };
    }

    internal static bool IsRaisesInvocation(this SemanticModel semanticModel, InvocationExpressionSyntax raisesInvocation, MoqKnownSymbols knownSymbols)
    {
        MemberAccessExpressionSyntax? raisesMethod = raisesInvocation.Expression as MemberAccessExpressionSyntax;

        if (raisesMethod == null)
        {
            return false;
        }

        string methodName = raisesMethod.Name.ToString();

        // First fast check before walking semantic model
        if (!string.Equals(methodName, "Raises", StringComparison.Ordinal)
            && !string.Equals(methodName, "RaisesAsync", StringComparison.Ordinal))
        {
            return false;
        }

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(raisesMethod);
        return symbolInfo.CandidateReason switch
        {
            CandidateReason.OverloadResolutionFailure => symbolInfo.CandidateSymbols.Any(symbol => symbol.IsMoqRaisesMethod(knownSymbols)),
            CandidateReason.None => IsRaisesSymbol(symbolInfo.Symbol, knownSymbols),
            _ => false,
        };
    }

    /// <summary>
    /// Determines if a conversion exists between two types (implicit, explicit, or identity).
    /// </summary>
    /// <param name="semanticModel">The semantic model to use for classification.</param>
    /// <param name="source">The source type symbol.</param>
    /// <param name="destination">The destination type symbol.</param>
    /// <returns><see langword="true"/> if a conversion exists; otherwise, <see langword="false"/>.</returns>
    internal static bool HasConversion(this SemanticModel semanticModel, ITypeSymbol source, ITypeSymbol destination)
    {
        Microsoft.CodeAnalysis.CSharp.Conversion conversion = semanticModel.Compilation.ClassifyConversion(source, destination);
        return conversion.Exists && (conversion.IsImplicit || conversion.IsExplicit || conversion.IsIdentity);
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

    private static bool IsCallbackOrReturnSymbol(ISymbol? symbol, MoqKnownSymbols knownSymbols)
    {
        if (symbol is null)
        {
            return false;
        }

        return symbol.IsMoqCallbackMethod(knownSymbols) || symbol.IsMoqReturnsMethod(knownSymbols);
    }

    private static bool IsRaisesSymbol(ISymbol? symbol, MoqKnownSymbols knownSymbols)
    {
        // First try symbol-based detection
        if (symbol?.IsMoqRaisesMethod(knownSymbols) == true)
        {
            return true;
        }

        // Fallback to string-based detection until symbol resolution is fully debugged
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

        // Check for IRaiseable and IRaiseableAsync interfaces
        bool isRaises = string.Equals(methodSymbol.Name, "Raises", StringComparison.Ordinal)
                         && containingTypeName.StartsWith("Moq.Language.IRaiseable", StringComparison.Ordinal);

        bool isRaisesAsync = string.Equals(methodSymbol.Name, "RaisesAsync", StringComparison.Ordinal)
                         && containingTypeName.StartsWith("Moq.Language.IRaiseableAsync", StringComparison.Ordinal);

        return isRaises || isRaisesAsync;
    }
}
