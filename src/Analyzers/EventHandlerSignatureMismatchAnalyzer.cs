using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Event handler types in SetupAdd, SetupRemove, and Raise should match the event signature.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventHandlerSignatureMismatchAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Event handler signature mismatch";
    private static readonly LocalizableString Message = "Event handler type should match the event signature";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.EventHandlerSignatureMismatch,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.EventHandlerSignatureMismatch}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        SemanticModel? semanticModel = invocationOperation.SemanticModel;
        if (semanticModel == null)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        if (!targetMethod.IsMoqEventMethod(knownSymbols))
        {
            return;
        }

        if (TryGetEventAndHandlerInfo(invocationOperation, out IEventSymbol? eventSymbol, out ITypeSymbol? handlerTypeSymbol))
        {
            ValidateEventHandlerMatch(context, invocationOperation, eventSymbol!, handlerTypeSymbol!);
        }
    }

    private static void ValidateEventHandlerMatch(OperationAnalysisContext context, IInvocationOperation invocationOperation, IEventSymbol eventSymbol, ITypeSymbol handlerTypeSymbol)
    {
        if (eventSymbol?.Type is not INamedTypeSymbol eventType ||
            handlerTypeSymbol is not INamedTypeSymbol handlerType ||
            SymbolEqualityComparer.Default.Equals(eventType.OriginalDefinition, handlerType.OriginalDefinition))
        {
            return;
        }

        if (eventType.TypeKind == TypeKind.Delegate &&
            handlerType.TypeKind == TypeKind.Delegate &&
            AreEventHandlerTypesCompatible(eventType, handlerType))
        {
            return;
        }

        Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Attempts to extract event and handler type information from Moq event method calls.
    /// </summary>
    private static bool TryGetEventAndHandlerInfo(IInvocationOperation invocation, out IEventSymbol? eventSymbol, out ITypeSymbol? handlerTypeSymbol)
    {
        eventSymbol = null;
        handlerTypeSymbol = null;

        if (invocation.Arguments.Length == 0)
        {
            return false;
        }

        // The first argument is typically the lambda expression referencing the event
        IOperation argumentOperation = invocation.Arguments[0].Value;
        argumentOperation = argumentOperation.WalkDownImplicitConversion();

        if (argumentOperation is IAnonymousFunctionOperation lambdaOperation)
        {
            // Look for event reference in the lambda body
            eventSymbol = GetEventSymbolFromLambda(lambdaOperation.Body);

            // For SetupAdd/SetupRemove, look for the handler type in It.IsAny<T>()
            // For Raise, the handler type is inferred from the event
            handlerTypeSymbol = GetHandlerTypeFromLambda(lambdaOperation.Body);

            return eventSymbol != null;
        }

        return false;
    }

    /// <summary>
    /// Extracts the event symbol from a lambda expression body.
    /// </summary>
    private static IEventSymbol? GetEventSymbolFromLambda(IOperation? operation)
    {
        if (operation == null)
        {
            return null;
        }

        // Look for event access patterns like "x.EventName +="
        if (operation is IBinaryOperation binaryOp &&
            binaryOp.LeftOperand is IEventReferenceOperation eventRef)
        {
            return eventRef.Event;
        }

        // Recursively search in child operations
        foreach (IOperation child in operation.ChildOperations)
        {
            IEventSymbol? result = GetEventSymbolFromLambda(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the handler type from a lambda expression, typically from It.IsAny&lt;THandler&gt;().
    /// </summary>
    private static ITypeSymbol? GetHandlerTypeFromLambda(IOperation? operation)
    {
        if (operation == null)
        {
            return null;
        }

        // Look for It.IsAny<THandler>() calls
        if (operation is IInvocationOperation invoc &&
            string.Equals(invoc.TargetMethod.Name, "IsAny", StringComparison.Ordinal) &&
            invoc.TargetMethod.IsGenericMethod &&
            invoc.TargetMethod.TypeArguments.Length == 1)
        {
            return invoc.TargetMethod.TypeArguments[0];
        }

        // Recursively search in child operations
        foreach (IOperation child in operation.ChildOperations)
        {
            ITypeSymbol? result = GetHandlerTypeFromLambda(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if two event handler delegate types are compatible.
    /// </summary>
    private static bool AreEventHandlerTypesCompatible(INamedTypeSymbol eventType, INamedTypeSymbol handlerType)
    {
        // For now, do a simple check - could be enhanced to check delegate signatures
        return SymbolEqualityComparer.Default.Equals(eventType.OriginalDefinition, handlerType.OriginalDefinition);
    }
}
