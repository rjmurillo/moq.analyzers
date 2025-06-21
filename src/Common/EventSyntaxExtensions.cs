using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers.Common;

/// <summary>
/// Provides extension methods for analyzing event syntax.
/// </summary>
public static class EventSyntaxExtensions
{
    /// <summary>
    /// Extracts the event type from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventSelector">The lambda event selector expression.</param>
    /// <param name="eventType">The extracted event type, if found.</param>
    /// <returns><c>true</c> if the event type was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetEventTypeFromLambdaSelector(
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
    /// Gets the parameter types for a given event delegate type.
    /// </summary>
    /// <param name="eventType">The event delegate type.</param>
    /// <returns>An array of parameter types expected by the event delegate.</returns>
    public static ITypeSymbol[] GetEventParameterTypes(ITypeSymbol eventType)
    {
        return GetEventParameterTypesInternal(eventType, null);
    }

    /// <summary>
    /// Validates that event arguments match the expected parameter types.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="eventArguments">The event arguments to validate.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="invocation">The invocation expression for error reporting.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    public static void ValidateEventArgumentTypes(
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
    /// Extracts arguments from an event method invocation.
    /// </summary>
    /// <param name="invocation">The method invocation.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventArguments">The extracted event arguments.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="eventTypeExtractor">Function to extract event type from the event selector.</param>
    /// <returns><c>true</c> if arguments were successfully extracted; otherwise, <c>false</c>.</returns>
    public static bool TryGetEventMethodArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out ArgumentSyntax[] eventArguments,
        out ITypeSymbol[] expectedParameterTypes,
        Func<SemanticModel, ExpressionSyntax, (bool Success, ITypeSymbol? EventType)> eventTypeExtractor)
    {
        return TryGetEventMethodArgumentsInternal(invocation, semanticModel, out eventArguments, out expectedParameterTypes, eventTypeExtractor, null);
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
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns><c>true</c> if arguments were successfully extracted; otherwise, <c>false</c>.</returns>
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

    private static ITypeSymbol[] GetEventParameterTypesInternal(ITypeSymbol eventType, KnownSymbols? knownSymbols)
    {
        // For delegates like Action<T>, we need to get the generic type arguments
        if (eventType is INamedTypeSymbol namedType)
        {
            // Handle Action delegates
            if (knownSymbols != null && namedType.IsActionDelegate(knownSymbols))
            {
                return namedType.TypeArguments.ToArray();
            }

            if (knownSymbols == null && IsActionDelegate(namedType))
            {
                return namedType.TypeArguments.ToArray();
            }

            // Handle EventHandler<T> - expects single argument of type T (not the sender/args pattern)
            if (knownSymbols != null && namedType.IsEventHandlerDelegate(knownSymbols))
            {
                return [namedType.TypeArguments[0]];
            }

            if (knownSymbols == null && IsEventHandlerDelegate(namedType))
            {
                return [namedType.TypeArguments[0]];
            }

            // Handle custom delegates by getting the Invoke method parameters
            IMethodSymbol? invokeMethod = namedType.DelegateInvokeMethod;
            if (invokeMethod != null)
            {
                return invokeMethod.Parameters.Select(p => p.Type).ToArray();
            }
        }

        return [];
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
