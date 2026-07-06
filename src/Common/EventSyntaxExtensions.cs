using Microsoft.CodeAnalysis.Operations;

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
    /// <param name="senderCanBeOmitted">
    /// When <see langword="true"/>, the delegate is EventHandler-shaped and Moq supplies the sender,
    /// so supplying one argument fewer than the parameter count (omitting the sender) is also valid.
    /// </param>
    /// <param name="eventArgsType">
    /// The <see cref="System.EventArgs"/> symbol used to model Moq's sender-supplying overload when
    /// <paramref name="senderCanBeOmitted"/> is <see langword="true"/>. May be <see langword="null"/>
    /// when the sender cannot be omitted.
    /// </param>
    /// <param name="invocation">The invocation expression for error reporting.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <param name="eventName">The event name to include in diagnostic messages.</param>
    internal static void ValidateEventArgumentTypes(
        this SyntaxNodeAnalysisContext context,
        ArgumentSyntax[] eventArguments,
        ITypeSymbol[] expectedParameterTypes,
        bool senderCanBeOmitted,
        INamedTypeSymbol? eventArgsType,
        InvocationExpressionSyntax invocation,
        DiagnosticDescriptor rule,
        string? eventName = null)
    {
        int offset = 0;

        // The sender may only be omitted when the EventArgs symbol is available to model Moq's
        // sender-supplying overload. That symbol is a precondition of EventHandler-shaped detection,
        // so this guard also keeps the offset == 1 path from dereferencing a null EventArgs type.
        if (senderCanBeOmitted && eventArgsType != null && eventArguments.Length == expectedParameterTypes.Length - 1)
        {
            // Moq's Raise/Raises(..., EventArgs) overloads supply the mock object as the sender for
            // EventHandler-shaped delegates. Validate the arguments against the post-sender parameters.
            System.Diagnostics.Debug.Assert(expectedParameterTypes.Length > 0, "EventHandler-shaped delegates have a sender parameter.");
            offset = 1;
        }
        else if (eventArguments.Length != expectedParameterTypes.Length)
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

            bool hasConversion;
            if (argumentType == null)
            {
                // A null-typed argument (e.g. a bare null literal) is treated as convertible to any
                // reference parameter, matching the prior behavior.
                hasConversion = true;
            }
            else if (offset == 1)
            {
                System.Diagnostics.Debug.Assert(eventArgsType != null, "The omitted-sender path requires the EventArgs symbol.");
                hasConversion = HasOmittedSenderConversion(
                    context.SemanticModel,
                    eventArguments[i].Expression,
                    argumentType,
                    eventArgsType!,
                    expectedParameterTypes[i + offset],
                    context.CancellationToken);
            }
            else
            {
                hasConversion = context.SemanticModel.HasConversion(argumentType, expectedParameterTypes[i]);
            }

            if (!hasConversion)
            {
                context.ReportDiagnostic(CreateEventDiagnostic(eventArguments[i].GetLocation(), rule, eventName));
            }
        }
    }

    /// <summary>
    /// Attempts to get the parameter types for a given event delegate type.
    /// </summary>
    /// <param name="eventType">The event delegate type to analyze.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <param name="expectedParameterTypes">
    /// The parameter types expected by the event delegate:
    /// - For <see cref="System.Action"/>/<see cref="System.Action{T}"/> delegates: all generic type arguments
    /// - For all other delegates (including <see cref="System.EventHandler"/>, <see cref="System.EventHandler{T}"/>,
    ///   and custom delegates): the full <c>Invoke</c> parameter list.
    /// </param>
    /// <param name="senderCanBeOmitted">
    /// <see langword="true"/> when the delegate is "EventHandler-shaped" — its <c>Invoke</c> signature is exactly
    /// <c>(object sender, TArgs e)</c> with <c>TArgs</c> being <see cref="System.EventArgs"/> or derived from it.
    /// Moq's <c>Raise(..., EventArgs)</c>/<c>Raises(..., EventArgs)</c> overloads supply the mock object as the
    /// sender for such delegates, so the sender argument may be omitted at the call site.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the delegate signature could be analyzed; <see langword="false"/> for
    /// unresolved (error) types, non-named types (e.g. type parameters), and non-delegate types.
    /// </returns>
    internal static bool TryGetEventParameterTypes(
        ITypeSymbol eventType,
        KnownSymbols knownSymbols,
        out ITypeSymbol[] expectedParameterTypes,
        out bool senderCanBeOmitted)
    {
        expectedParameterTypes = [];
        senderCanBeOmitted = false;

        // Error types (mid-edit code) and non-named types (e.g. type parameters, arrays) cannot be
        // analyzed. Report failure instead of pretending the delegate has zero parameters.
        if (eventType.TypeKind == TypeKind.Error || eventType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // System.Action / System.Action<T>: the type arguments are the parameters.
        // NOTE: Action`2 and higher arities are intentionally resolved through the Invoke
        // signature below, exactly as before (IsActionDelegate only matches Action and Action`1).
        if (namedType.IsActionDelegate(knownSymbols))
        {
            expectedParameterTypes = namedType.TypeArguments.ToArray();
            return true;
        }

        // Any other delegate: read the Invoke signature.
        IMethodSymbol? invokeMethod = namedType.DelegateInvokeMethod;
        if (invokeMethod is null)
        {
            // Not a delegate type: unanalyzable.
            return false;
        }

        ImmutableArray<IParameterSymbol> parameters = invokeMethod.Parameters;
        ITypeSymbol[] types = new ITypeSymbol[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            types[i] = parameters[i].Type;
        }

        expectedParameterTypes = types;
        senderCanBeOmitted = IsEventHandlerShaped(namedType, parameters, knownSymbols);
        return true;
    }

    /// <summary>
    /// Extracts arguments from an event method invocation.
    /// </summary>
    /// <param name="invocation">The method invocation.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="eventArguments">The extracted event arguments.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="senderCanBeOmitted">Whether the sender argument may be omitted (EventHandler-shaped delegates).</param>
    /// <param name="eventTypeExtractor">Function to extract event type from the event selector.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns><see langword="true" /> if arguments were successfully extracted; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventMethodArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out ArgumentSyntax[] eventArguments,
        out ITypeSymbol[] expectedParameterTypes,
        out bool senderCanBeOmitted,
        Func<SemanticModel, ExpressionSyntax, (bool Success, ITypeSymbol? EventType)> eventTypeExtractor,
        KnownSymbols knownSymbols)
    {
        eventArguments = [];
        expectedParameterTypes = [];
        senderCanBeOmitted = false;

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

        if (!TryGetEventParameterTypes(eventType, knownSymbols, out expectedParameterTypes, out senderCanBeOmitted))
        {
            // The delegate type could not be analyzed (e.g. an error type in mid-edit code).
            // Do not report diagnostics based on a guessed signature.
            return false;
        }

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
    /// Extracts event arguments from an event method invocation using the standard
    /// lambda-based event type extraction pattern shared by Raise and Raises analyzers.
    /// </summary>
    /// <param name="invocation">The method invocation.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <param name="eventArguments">The extracted event arguments.</param>
    /// <param name="expectedParameterTypes">The expected parameter types.</param>
    /// <param name="senderCanBeOmitted">Whether the sender argument may be omitted (EventHandler-shaped delegates).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true" /> if arguments were successfully extracted; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetEventMethodArgumentsFromLambdaSelector(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        KnownSymbols knownSymbols,
        out ArgumentSyntax[] eventArguments,
        out ITypeSymbol[] expectedParameterTypes,
        out bool senderCanBeOmitted,
        CancellationToken cancellationToken = default)
    {
        return TryGetEventMethodArguments(
            invocation,
            semanticModel,
            out eventArguments,
            out expectedParameterTypes,
            out senderCanBeOmitted,
            (sm, selector) =>
            {
                bool success = sm.TryGetEventTypeFromLambdaSelector(selector, out ITypeSymbol? eventType, cancellationToken);
                return (success, eventType);
            },
            knownSymbols);
    }

    /// <summary>
    /// Runs the shared Raise/Raises event argument validation tail.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="invocation">The Raise or Raises invocation.</param>
    /// <param name="knownSymbols">The known Moq symbols for this compilation.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    internal static void AnalyzeEventArgumentsAgainstEventSignature(
        this SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        MoqKnownSymbols knownSymbols,
        DiagnosticDescriptor rule)
    {
        if (!TryGetEventMethodArgumentsFromLambdaSelector(
            invocation,
            context.SemanticModel,
            knownSymbols,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            out bool senderCanBeOmitted,
            context.CancellationToken))
        {
            return;
        }

        string eventName = GetEventNameFromSelector(invocation, context.SemanticModel, context.CancellationToken);

        context.ValidateEventArgumentTypes(
            eventArguments,
            expectedParameterTypes,
            senderCanBeOmitted,
            knownSymbols.EventArgs,
            invocation,
            rule,
            eventName);
    }

    /// <summary>
    /// Extracts the event name from the first argument (event selector lambda) of an invocation.
    /// </summary>
    /// <param name="invocation">The method invocation containing the lambda selector.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The event name if found; otherwise "event" as a fallback.</returns>
    internal static string GetEventNameFromSelector(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken = default)
    {
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 1)
        {
            return "event";
        }

        ExpressionSyntax eventSelector = arguments[0].Expression;

        return semanticModel.TryGetEventNameFromLambdaSelector(eventSelector, out string? eventName, cancellationToken)
            ? eventName!
            : "event";
    }

    /// <summary>
    /// Creates a <see cref="Diagnostic"/> for an event-related rule violation.
    /// When <paramref name="eventName"/> is provided, it is passed as a message format argument.
    /// When <paramref name="eventName"/> is <see langword="null"/>, no message arguments are included.
    /// </summary>
    /// <param name="location">The source location for the diagnostic.</param>
    /// <param name="rule">The diagnostic descriptor for the rule.</param>
    /// <param name="eventName">The event name to include in the message, or <see langword="null"/>.</param>
    /// <returns>A new <see cref="Diagnostic"/> instance.</returns>
    internal static Diagnostic CreateEventDiagnostic(Location location, DiagnosticDescriptor rule, string? eventName)
    {
        return eventName != null
            ? location.CreateDiagnostic(rule, eventName)
            : location.CreateDiagnostic(rule);
    }

    /// <summary>
    /// Determines whether an omitted-sender payload is valid for Moq's sender-supplying overload.
    /// </summary>
    /// <param name="semanticModel">The semantic model used for conversion classification.</param>
    /// <param name="payloadExpression">The single payload argument expression.</param>
    /// <param name="payloadType">The static type of the single payload argument.</param>
    /// <param name="eventArgsType">The <see cref="System.EventArgs"/> symbol.</param>
    /// <param name="delegateArgumentType">The delegate's post-sender parameter type.</param>
    /// <param name="cancellationToken">A token to observe while resolving the payload operation.</param>
    /// <returns><see langword="true"/> if the payload is valid; otherwise <see langword="false"/>.</returns>
    private static bool HasOmittedSenderConversion(
        SemanticModel semanticModel,
        ExpressionSyntax payloadExpression,
        ITypeSymbol payloadType,
        INamedTypeSymbol eventArgsType,
        ITypeSymbol delegateArgumentType,
        CancellationToken cancellationToken)
    {
        // Overload selection: Moq's Raise/Raises(Action<T>, EventArgs) overload is chosen only when the
        // payload is IMPLICITLY convertible to EventArgs. An explicit-only conversion (e.g. object to
        // EventArgs) binds the params-object overload instead, so Moq never supplies the sender and the
        // one-argument form throws at runtime. Report those cases.
        if (!semanticModel.HasImplicitConversion(payloadType, eventArgsType))
        {
            return false;
        }

        // A user-defined conversion from the payload type to the EventArgs parameter runs its operator at
        // the call site, so Moq receives an instance of the operator's return type, not the payload's own
        // type. The exact runtime type is not statically known (the operator may return a subtype), so
        // tolerate a reference or identity conversion from that return type to the delegate argument type.
        if (semanticModel.TryGetUserDefinedConversionReturnType(payloadType, eventArgsType, out ITypeSymbol? convertedType))
        {
            System.Diagnostics.Debug.Assert(convertedType != null, "A user-defined conversion has a non-null operator return type.");
            return semanticModel.HasReferenceOrIdentityConversion(convertedType!, delegateArgumentType);
        }

        // Runtime compatibility: once the sender-supplying overload is chosen, Moq casts the payload to
        // the delegate's post-sender parameter type via reflection (a reference cast, never a user-defined
        // conversion). When the payload's exact runtime type is statically known (an object-creation
        // expression such as new EventArgs(), or the well-known System.EventArgs.Empty singleton), the cast
        // succeeds only if that exact type is the delegate argument type or derives from it. A base
        // EventArgs instance supplied for a derived-args delegate throws at runtime, so it is reported.
        if (TryGetKnownExactType(semanticModel, payloadExpression, eventArgsType, cancellationToken, out ITypeSymbol? exactType))
        {
            System.Diagnostics.Debug.Assert(exactType != null, "A known-exact payload type is non-null.");
            return IsOrDerivesFrom(exactType!, delegateArgumentType);
        }

        // Runtime type unknown (locals, parameters, properties, method results, casts): a base EventArgs
        // value can legitimately hold a derived instance, so a reference downcast is tolerated (matching
        // the full-argument path). User-defined conversions are excluded because Moq's reflection cast
        // never invokes them. A payload with no reference conversion to the delegate argument type
        // (e.g. an unrelated EventArgs subclass) can never bind and is reported.
        return semanticModel.HasReferenceOrIdentityConversion(payloadType, delegateArgumentType);
    }

    /// <summary>
    /// Attempts to resolve the payload's statically-known exact runtime type. This is possible only when
    /// the payload is an object-creation expression (<c>new T(...)</c> constructs an instance of exactly
    /// <c>T</c>) or the well-known <see cref="System.EventArgs.Empty"/> singleton (a base
    /// <see cref="System.EventArgs"/> instance).
    /// </summary>
    /// <param name="semanticModel">The semantic model used to resolve the payload operation.</param>
    /// <param name="payloadExpression">The payload argument expression.</param>
    /// <param name="eventArgsType">The <see cref="System.EventArgs"/> symbol.</param>
    /// <param name="cancellationToken">A token to observe while resolving the payload operation.</param>
    /// <param name="exactType">The resolved exact runtime type, when known.</param>
    /// <returns><see langword="true"/> when the exact runtime type is known; otherwise <see langword="false"/>.</returns>
    private static bool TryGetKnownExactType(
        SemanticModel semanticModel,
        ExpressionSyntax payloadExpression,
        INamedTypeSymbol eventArgsType,
        CancellationToken cancellationToken,
        out ITypeSymbol? exactType)
    {
        IOperation? operation = semanticModel.GetOperation(payloadExpression, cancellationToken);

        // Reference and identity conversions (upcasts, downcasts, boxing) preserve object identity, so the
        // wrapped expression's runtime type flows through unchanged. Unwrap them to inspect the underlying
        // expression. A user-defined conversion is NOT unwrapped: it invokes an operator that constructs a
        // new instance of the target type, so the operand's type is not the runtime type Moq receives.
        while (operation is IConversionOperation conversion && conversion.OperatorMethod is null)
        {
            operation = conversion.Operand;
        }

        // An object-creation expression constructs an instance of exactly its created type.
        if (operation is IObjectCreationOperation creation && creation.Type != null)
        {
            exactType = creation.Type;
            return true;
        }

        // System.EventArgs.Empty is a base EventArgs singleton.
        if (operation is IFieldReferenceOperation fieldReference && IsEventArgsEmptyField(fieldReference.Field, eventArgsType))
        {
            exactType = eventArgsType;
            return true;
        }

        exactType = null;
        return false;
    }

    /// <summary>
    /// Determines whether a field symbol is the well-known <see cref="System.EventArgs.Empty"/> field.
    /// </summary>
    /// <param name="field">The field symbol to test.</param>
    /// <param name="eventArgsType">The <see cref="System.EventArgs"/> symbol.</param>
    /// <returns><see langword="true"/> when the field is <see cref="System.EventArgs.Empty"/>; otherwise <see langword="false"/>.</returns>
    private static bool IsEventArgsEmptyField(IFieldSymbol field, INamedTypeSymbol eventArgsType)
    {
        foreach (ISymbol member in eventArgsType.GetMembers("Empty"))
        {
            if (member is IFieldSymbol && SymbolEqualityComparer.Default.Equals(member, field))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a type is the specified base type or derives from it.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="baseType">The candidate base type.</param>
    /// <returns><see langword="true"/> when <paramref name="type"/> is or derives from <paramref name="baseType"/>; otherwise <see langword="false"/>.</returns>
    private static bool IsOrDerivesFrom(ITypeSymbol type, ITypeSymbol baseType)
    {
        foreach (ITypeSymbol current in type.GetBaseTypesAndThis())
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a delegate is "EventHandler-shaped": <c>(object sender, TArgs e)</c> with
    /// <c>TArgs</c> being <see cref="System.EventArgs"/> or derived from it. Moq's
    /// <c>Raise(Action{T}, EventArgs)</c> and <c>Raises(Action{T}, EventArgs)</c> overloads invoke such
    /// delegates as <c>handler(mock.Object, args)</c>, so the sender argument may be omitted by callers.
    /// </summary>
    /// <param name="namedType">The delegate type.</param>
    /// <param name="invokeParameters">The delegate's <c>Invoke</c> parameters.</param>
    /// <param name="knownSymbols">Known symbols for type checking.</param>
    /// <returns><see langword="true"/> if the delegate is EventHandler-shaped; otherwise <see langword="false"/>.</returns>
    private static bool IsEventHandlerShaped(
        INamedTypeSymbol namedType,
        ImmutableArray<IParameterSymbol> invokeParameters,
        KnownSymbols knownSymbols)
    {
        // Fast path: the non-generic System.EventHandler delegate itself.
        if (knownSymbols.EventHandler is not null
            && SymbolEqualityComparer.Default.Equals(namedType, knownSymbols.EventHandler))
        {
            return true;
        }

        if (invokeParameters.Length != 2
            || invokeParameters[0].Type.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        INamedTypeSymbol? eventArgsType = knownSymbols.EventArgs;
        if (eventArgsType is null)
        {
            return false;
        }

        foreach (ITypeSymbol baseType in invokeParameters[1].Type.GetBaseTypesAndThis())
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, eventArgsType))
            {
                return true;
            }
        }

        return false;
    }
}
