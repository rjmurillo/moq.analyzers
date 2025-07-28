namespace Moq.Analyzers.Common;

/// <summary>
/// Provides extension methods for analyzing event syntax.
/// </summary>
internal static class EventSyntaxExtensions
{
    /// <summary>
    /// Extracts the event name from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="eventName">The extracted event name, if found.</param>
    /// <returns><see langword="true" /> if the event name was found; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventNameFromLambdaSelector(
        SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        out string? eventName)
    {
        eventName = null;

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
        if (symbolInfo.Symbol is not IEventSymbol eventSymbol)
        {
            return false;
        }

        eventName = eventSymbol.Name;
        return true;
    }

    /// <summary>
    /// Extracts the event type from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="eventType">The extracted event type, if found.</param>
    /// <returns><see langword="true" /> if the event type was found; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventTypeFromLambdaSelector(
        SemanticModel semanticModel,
        ExpressionSyntax eventSelector,
        out ITypeSymbol? eventType)
    {
        eventType = null;

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
        if (symbolInfo.Symbol is not IEventSymbol eventSymbol)
        {
            return false;
        }

        eventType = eventSymbol.Type;
        return true;
    }

    /// <summary>
    /// Validates that event arguments match the expected parameter types.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="eventArguments">The event arguments to validate.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="invocation">The invocation expression for error reporting.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    internal static void ValidateEventArgumentTypes(
        SyntaxNodeAnalysisContext context,
        ArgumentSyntax[] eventArguments,
        ITypeSymbol[] expectedParameterTypes,
        InvocationExpressionSyntax invocation,
        DiagnosticDescriptor rule)
    {
        if (eventArguments.Length != expectedParameterTypes.Length)
        {
            Location location;
            if (eventArguments.Length < expectedParameterTypes.Length)
            {
                // Too few arguments: report on the invocation
                location = invocation.GetLocation();
            }
            else
            {
                // Too many arguments: report on the first extra argument
                location = eventArguments[expectedParameterTypes.Length].GetLocation();
            }

            Diagnostic diagnostic = location.CreateDiagnostic(rule);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check each argument type matches the expected parameter type
        for (int i = 0; i < eventArguments.Length; i++)
        {
            TypeInfo argumentTypeInfo = context.SemanticModel.GetTypeInfo(eventArguments[i].Expression, context.CancellationToken);
            ITypeSymbol? argumentType = argumentTypeInfo.Type;
            ITypeSymbol expectedType = expectedParameterTypes[i];

            if (argumentType != null && !context.SemanticModel.HasConversion(argumentType, expectedType))
            {
                // Report on the specific argument with the wrong type
                Diagnostic diagnostic = eventArguments[i].GetLocation().CreateDiagnostic(rule);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Gets the parameter types for a given event delegate type.
    /// </summary>
    /// <param name="eventType">The event delegate type.</param>
    /// <returns>An array of parameter types expected by the event delegate.</returns>
    internal static ITypeSymbol[] GetEventParameterTypes(ITypeSymbol eventType)
    {
        return GetEventParameterTypesInternal(eventType, null);
    }

    /// <summary>
    /// Gets the parameter types for a given event delegate type.
    /// </summary>
    /// <param name="eventType">The event delegate type.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns>An array of parameter types expected by the event delegate.</returns>
    internal static ITypeSymbol[] GetEventParameterTypes(ITypeSymbol eventType, KnownSymbols knownSymbols)
    {
        return GetEventParameterTypesInternal(eventType, knownSymbols);
    }

    /// <summary>
    /// Extracts arguments from an event method invocation.
    /// </summary>
    /// <param name="invocation">The method invocation.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventArguments">The extracted event arguments.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="eventTypeExtractor">Function to extract event type from the event selector.</param>
    /// <returns><see langword="true" /> if arguments were successfully extracted; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventMethodArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out ArgumentSyntax[] eventArguments,
        out ITypeSymbol[] expectedParameterTypes,
        Func<SemanticModel, ExpressionSyntax, (bool Success, ITypeSymbol? EventType)> eventTypeExtractor)
    {
        return TryGetEventMethodArgumentsInternal(invocation, semanticModel, out eventArguments, out expectedParameterTypes, eventTypeExtractor, null);
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
        return TryGetEventMethodArgumentsInternal(invocation, semanticModel, out eventArguments, out expectedParameterTypes, eventTypeExtractor, knownSymbols);
    }

    private static bool TryGetEventMethodArgumentsInternal(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out ArgumentSyntax[] eventArguments,
        out ITypeSymbol[] expectedParameterTypes,
        Func<SemanticModel, ExpressionSyntax, (bool Success, ITypeSymbol? EventType)> eventTypeExtractor,
        KnownSymbols? knownSymbols)
    {
        eventArguments = [];
        expectedParameterTypes = [];

        // Get the arguments to the method
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // Method should have at least 1 argument (the event selector)
        if (arguments.Count < 1)
        {
            return false;
        }

        // First argument should be a lambda that selects the event
        ExpressionSyntax eventSelector = arguments[0].Expression;
        (bool success, ITypeSymbol? eventType) = eventTypeExtractor(semanticModel, eventSelector);
        if (!success || eventType == null)
        {
            return false;
        }

        // Get expected parameter types from the event delegate
        expectedParameterTypes = knownSymbols != null ? GetEventParameterTypes(eventType, knownSymbols) : GetEventParameterTypes(eventType);

        // The remaining arguments should match the event parameter types
        if (arguments.Count <= 1)
        {
            eventArguments = [];
        }
        else
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
    /// Gets the parameter types for a given event delegate type.
    /// This method handles various delegate types including Action delegates, EventHandler delegates,
    /// and custom delegates by analyzing their structure and extracting parameter information.
    /// </summary>
    /// <param name="eventType">The event delegate type to analyze.</param>
    /// <param name="knownSymbols">Known symbols for enhanced type checking and recognition.</param>
    /// <returns>
    /// An array of parameter types expected by the event delegate:
    /// - For Action delegates: Returns all generic type arguments
    /// - For EventHandler&lt;T&gt; delegates: Returns the single generic argument T
    /// - For custom delegates: Returns parameters from the Invoke method
    /// - For non-delegate types: Returns empty array.
    /// </returns>
    private static ITypeSymbol[] GetEventParameterTypesInternal(ITypeSymbol eventType, KnownSymbols? knownSymbols)
    {
        if (eventType is not INamedTypeSymbol namedType)
        {
            return [];
        }

        // Try different delegate type handlers in order of specificity
        ITypeSymbol[]? parameterTypes = TryGetActionDelegateParameters(namedType, knownSymbols) ??
                            TryGetEventHandlerDelegateParameters(namedType, knownSymbols) ??
                            TryGetCustomDelegateParameters(namedType);

        return parameterTypes ?? [];
    }

    /// <summary>
    /// Attempts to get parameter types from Action delegate types.
    /// </summary>
    /// <param name="namedType">The named type symbol to check.</param>
    /// <param name="knownSymbols">Optional known symbols for enhanced type checking.</param>
    /// <returns>Parameter types if this is an Action delegate; otherwise null.</returns>
    private static ITypeSymbol[]? TryGetActionDelegateParameters(INamedTypeSymbol namedType, KnownSymbols? knownSymbols)
    {
        bool isActionDelegate = knownSymbols != null
            ? namedType.IsActionDelegate(knownSymbols)
            : IsActionDelegate(namedType);

        return isActionDelegate ? namedType.TypeArguments.ToArray() : null;
    }

    /// <summary>
    /// Attempts to get parameter types from EventHandler delegate types.
    /// </summary>
    /// <param name="namedType">The named type symbol to check.</param>
    /// <param name="knownSymbols">Optional known symbols for enhanced type checking.</param>
    /// <returns>Parameter types if this is an EventHandler delegate; otherwise null.</returns>
    private static ITypeSymbol[]? TryGetEventHandlerDelegateParameters(INamedTypeSymbol namedType, KnownSymbols? knownSymbols)
    {
        bool isEventHandlerDelegate = knownSymbols != null
            ? namedType.IsEventHandlerDelegate(knownSymbols)
            : IsEventHandlerDelegate(namedType);

        if (isEventHandlerDelegate && namedType.TypeArguments.Length > 0)
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

    private static bool IsActionDelegate(INamedTypeSymbol namedType)
    {
        return string.Equals(namedType.Name, "Action", StringComparison.Ordinal);
    }

    private static bool IsEventHandlerDelegate(INamedTypeSymbol namedType)
    {
        return string.Equals(namedType.Name, "EventHandler", StringComparison.Ordinal) && namedType.TypeArguments.Length == 1;
    }
}
