namespace Moq.Analyzers.Common;

/// <summary>
/// Provides extension methods for analyzing event syntax.
/// </summary>
internal static class EventSyntaxExtensions
{
    /// <summary>
    /// Validates that event arguments match the expected parameter types.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="eventArguments">The event arguments to validate.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="invocation">The invocation expression for error reporting.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <param name="eventName">The event name to include in diagnostic messages.</param>
    internal static void ValidateEventArgumentTypes(
        SyntaxNodeAnalysisContext context,
        ArgumentSyntax[] eventArguments,
        ITypeSymbol[] expectedParameterTypes,
        InvocationExpressionSyntax invocation,
        DiagnosticDescriptor rule,
        string? eventName)
    {
        if (eventArguments.Length != expectedParameterTypes.Length)
        {
            Location location = eventArguments.Length < expectedParameterTypes.Length
                ? invocation.GetLocation()
                : eventArguments[expectedParameterTypes.Length].GetLocation();

            context.ReportDiagnostic(CreateEventDiagnostic(location, rule, eventName));
            return;
        }

        for (int i = 0; i < eventArguments.Length; i++)
        {
            TypeInfo argumentTypeInfo = context.SemanticModel.GetTypeInfo(eventArguments[i].Expression, context.CancellationToken);
            ITypeSymbol? argumentType = argumentTypeInfo.Type;

            if (argumentType != null && !context.SemanticModel.HasConversion(argumentType, expectedParameterTypes[i]))
            {
                context.ReportDiagnostic(CreateEventDiagnostic(eventArguments[i].GetLocation(), rule, eventName));
            }
        }
    }

    /// <summary>
    /// Gets the parameter types for a given event delegate type.
    /// </summary>
    /// <param name="eventType">The event delegate type.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns>An array of parameter types expected by the event delegate.</returns>
    internal static ITypeSymbol[] GetEventParameterTypes(ITypeSymbol eventType, KnownSymbols knownSymbols)
    {
        if (eventType is not INamedTypeSymbol namedType)
        {
            return [];
        }

        ITypeSymbol[]? parameterTypes = TryGetActionDelegateParameters(namedType, knownSymbols) ??
                            TryGetEventHandlerDelegateParameters(namedType, knownSymbols) ??
                            TryGetCustomDelegateParameters(namedType);

        return parameterTypes ?? [];
    }

    /// <summary>
    /// Extracts arguments from an event method invocation.
    /// </summary>
    /// <param name="invocation">The method invocation.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventArguments">The extracted event arguments.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="eventTypeExtractor">Function to extract event type from the event selector.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns><see langword="true" /> if arguments were successfully extracted; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventMethodArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out ArgumentSyntax[] eventArguments,
        out ITypeSymbol[] expectedParameterTypes,
        Func<SemanticModel, ExpressionSyntax, (bool Success, ITypeSymbol? EventType)> eventTypeExtractor,
        KnownSymbols knownSymbols)
    {
        eventArguments = [];
        expectedParameterTypes = [];

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        if (arguments.Count < 1)
        {
            return false;
        }

        ExpressionSyntax eventSelector = arguments[0].Expression;
        (bool success, ITypeSymbol? eventType) = eventTypeExtractor(semanticModel, eventSelector);
        if (!success || eventType == null)
        {
            return false;
        }

        expectedParameterTypes = GetEventParameterTypes(eventType, knownSymbols);

        if (arguments.Count > 1)
        {
            eventArguments = new ArgumentSyntax[arguments.Count - 1];
            for (int i = 1; i < arguments.Count; i++)
            {
                eventArguments[i - 1] = arguments[i];
            }
        }

        return true;
    }

    /// <summary>
    /// Attempts to get parameter types from Action delegate types.
    /// </summary>
    /// <param name="namedType">The named type symbol to check.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns>Parameter types if this is an Action delegate; otherwise null.</returns>
    private static ITypeSymbol[]? TryGetActionDelegateParameters(INamedTypeSymbol namedType, KnownSymbols knownSymbols)
    {
        return namedType.IsActionDelegate(knownSymbols) ? namedType.TypeArguments.ToArray() : null;
    }

    /// <summary>
    /// Attempts to get parameter types from EventHandler delegate types.
    /// </summary>
    /// <param name="namedType">The named type symbol to check.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns>Parameter types if this is an EventHandler delegate; otherwise null.</returns>
    private static ITypeSymbol[]? TryGetEventHandlerDelegateParameters(INamedTypeSymbol namedType, KnownSymbols knownSymbols)
    {
        if (namedType.IsEventHandlerDelegate(knownSymbols) && namedType.TypeArguments.Length > 0)
        {
            return [namedType.TypeArguments[0]];
        }

        return null;
    }

    /// <summary>
    /// Attempts to get parameter types from custom delegate types using the Invoke method.
    /// </summary>
    /// <param name="namedType">The named type symbol to check.</param>
    /// <returns>Parameter types if this has a delegate Invoke method; otherwise null.</returns>
    private static ITypeSymbol[]? TryGetCustomDelegateParameters(INamedTypeSymbol namedType)
    {
        IMethodSymbol? invokeMethod = namedType.DelegateInvokeMethod;
        return invokeMethod?.Parameters.Select(p => p.Type).ToArray();
    }

    private static Diagnostic CreateEventDiagnostic(Location location, DiagnosticDescriptor rule, string? eventName)
    {
        return eventName != null
            ? location.CreateDiagnostic(rule, eventName)
            : location.CreateDiagnostic(rule);
    }
}
