namespace Moq.Analyzers;

/// <summary>
/// Raises event arguments should match the event delegate signature.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RaisesEventArgumentsShouldMatchEventSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Raises event arguments should match event signature";
    private static readonly LocalizableString Message = "Raises event arguments should match the event delegate signature";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a Raises method call
        if (!IsRaisesMethodCall(invocation))
        {
            return;
        }

        if (!TryGetRaisesMethodArguments(invocation, context.SemanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
        {
            return;
        }

        ValidateArgumentTypes(context, eventArguments, expectedParameterTypes, invocation);
    }

    private static bool IsRaisesMethodCall(InvocationExpressionSyntax invocation)
    {
        // Check if the method being called is named "Raises"
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
!string.Equals(memberAccess.Name.Identifier.ValueText, "Raises", StringComparison.Ordinal))
        {
            return false;
        }

        // Additional validation could be added here to ensure it's a Moq Raises method
        // For now, we'll rely on the method name
        return true;
    }

    private static bool TryGetRaisesMethodArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes)
    {
        eventArguments = Array.Empty<ArgumentSyntax>();
        expectedParameterTypes = Array.Empty<ITypeSymbol>();

        // Get the arguments to the Raises method
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // Raises method should have at least 1 argument (the event selector)
        if (arguments.Count < 1)
        {
            return false;
        }

        // First argument should be a lambda that selects the event
        ExpressionSyntax eventSelector = arguments[0].Expression;
        if (!Moq.Analyzers.Common.EventSyntaxExtensions.TryGetEventTypeFromLambdaSelector(semanticModel, eventSelector, out ITypeSymbol? eventType))
        {
            return false;
        }

        // Get expected parameter types from the event delegate
        expectedParameterTypes = GetEventParameterTypes(eventType!);

        // The remaining arguments should match the event parameter types
#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
        eventArguments = arguments.Skip(1).ToArray();
#pragma warning restore ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing

        return true;
    }

    

    private static ITypeSymbol[] GetEventParameterTypes(ITypeSymbol eventType)
    {
        // For delegates like Action<T>, we need to get the generic type arguments
        if (eventType is INamedTypeSymbol namedType)
        {
            // Handle Action delegates
            if (IsActionDelegate(namedType))
            {
                return namedType.TypeArguments.ToArray();
            }

            // Handle EventHandler<T> - expects single argument of type T (not the sender/args pattern)
            if (IsEventHandlerDelegate(namedType))
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

        return Array.Empty<ITypeSymbol>();
    }

    private static void ValidateArgumentTypes(SyntaxNodeAnalysisContext context, ArgumentSyntax[] eventArguments, ITypeSymbol[] expectedParameterTypes, InvocationExpressionSyntax invocation)
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

            Diagnostic diagnostic = location.CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check each argument type matches the expected parameter type
        for (int i = 0; i < eventArguments.Length; i++)
        {
            TypeInfo argumentTypeInfo = context.SemanticModel.GetTypeInfo(eventArguments[i].Expression, context.CancellationToken);
            ITypeSymbol? argumentType = argumentTypeInfo.Type;
            ITypeSymbol expectedType = expectedParameterTypes[i];

            if (argumentType != null && !HasConversion(context.SemanticModel, argumentType, expectedType))
            {
                // Report on the specific argument with the wrong type
                Diagnostic diagnostic = eventArguments[i].GetLocation().CreateDiagnostic(Rule);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsActionDelegate(INamedTypeSymbol namedType)
    {
        return string.Equals(namedType.Name, "Action", StringComparison.Ordinal);
    }

    private static bool IsEventHandlerDelegate(INamedTypeSymbol namedType)
    {
        return string.Equals(namedType.Name, "EventHandler", StringComparison.Ordinal) && namedType.TypeArguments.Length == 1;
    }

    private static bool HasConversion(SemanticModel semanticModel, ITypeSymbol source, ITypeSymbol destination)
    {
        return semanticModel.HasConversion(source, destination);
    }
}
