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
        if (raisesInvocation.Expression is not MemberAccessExpressionSyntax raisesMethod)
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

    /// <summary>
    /// Extracts the event name from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="eventName">The extracted event name, if found.</param>
    /// <returns><see langword="true" /> if the event name was found; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventNameFromLambdaSelector(
        this SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        out string? eventName)
    {
        return TryGetEventPropertyFromLambdaSelector(semanticModel, eventSelector, static eventSymbol => eventSymbol.ToDisplayString(), out eventName);
    }

    /// <summary>
    /// Extracts the event type from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="eventType">The extracted event type, if found.</param>
    /// <returns><see langword="true" /> if the event type was found; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventTypeFromLambdaSelector(
        this SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        out ITypeSymbol? eventType)
    {
        return TryGetEventPropertyFromLambdaSelector(semanticModel, eventSelector, static eventSymbol => eventSymbol.Type, out eventType);
    }

    /// <summary>
    /// Extracts a property from an event symbol found in a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <typeparam name="T">The type of the property to extract.</typeparam>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="propertySelector">A function to extract the desired property from the event symbol.</param>
    /// <param name="result">The extracted property, if found.</param>
    /// <returns><see langword="true" /> if the property was extracted; otherwise, <see langword="false" />.</returns>
    private static bool TryGetEventPropertyFromLambdaSelector<T>(
        SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        Func<IEventSymbol, T> propertySelector,
        out T? result)
    {
        result = default;

        if (TryGetEventSymbolFromLambdaSelector(semanticModel, eventSelector, out IEventSymbol? eventSymbol) &&
            eventSymbol != null)
        {
            result = propertySelector(eventSymbol);
            return true;
        }

        return false;
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

    /// <summary>
    /// Extracts the event symbol from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="eventSymbol">The extracted event symbol, if found.</param>
    /// <returns><see langword="true" /> if the event symbol was found; otherwise, <see langword="false" />.</returns>
    private static bool TryGetEventSymbolFromLambdaSelector(
        SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        out IEventSymbol? eventSymbol)
    {
        eventSymbol = null;

        // The event selector should be a lambda like: p => p.EventName += null
        if (eventSelector is not LambdaExpressionSyntax lambda)
        {
            return false;
        }

        // The body should be an assignment expression with += operator
        if (lambda.Body is not AssignmentExpressionSyntax assignment ||
            !assignment.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken))
        {
            return false;
        }

        // The left side should be a member access to the event
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Get the symbol for the event
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is not IEventSymbol symbol)
        {
            return false;
        }

        eventSymbol = symbol;
        return true;
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
        return symbol?.IsMoqRaisesMethod(knownSymbols) == true;
    }
}
