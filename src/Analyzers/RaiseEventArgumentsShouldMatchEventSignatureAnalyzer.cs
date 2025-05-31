namespace Moq.Analyzers;

/// <summary>
/// Raise event arguments should match the event delegate signature.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RaiseEventArgumentsShouldMatchEventSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Raise event arguments should match event signature";
    private static readonly LocalizableString Message = "Raise event arguments should match the event delegate signature";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature}.md");

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
        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);

        // Check if this is a Raise method call on a Mock<T>
        if (!IsRaiseMethodCall(context.SemanticModel, invocation, knownSymbols))
        {
            return;
        }

        // Get the arguments to the Raise method
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // Raise method should have at least 1 argument (the event selector)
        if (arguments.Count < 1)
        {
            return;
        }

        // First argument should be a lambda that selects the event
        ExpressionSyntax eventSelector = arguments[0].Expression;
        if (!TryGetEventTypeFromSelector(context.SemanticModel, eventSelector, out ITypeSymbol? eventType))
        {
            return;
        }

        // Get expected parameter types from the event delegate
        ITypeSymbol[] expectedParameterTypes = GetEventParameterTypes(eventType!);

        // The remaining arguments should match the event parameter types
#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
        ArgumentSyntax[] eventArguments = arguments.Skip(1).ToArray();
#pragma warning restore ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing

        if (eventArguments.Length != expectedParameterTypes.Length)
        {
            // Wrong number of arguments
            Location location = eventArguments.Length > 0 ? eventArguments[0].GetLocation() : invocation.ArgumentList.GetLocation();
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
                Diagnostic diagnostic = eventArguments[i].CreateDiagnostic(Rule);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsRaiseMethodCall(SemanticModel semanticModel, InvocationExpressionSyntax invocation, MoqKnownSymbols knownSymbols)
    {
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        return knownSymbols.Mock1Raise.Contains(methodSymbol.OriginalDefinition);
    }

    private static bool TryGetEventTypeFromSelector(SemanticModel semanticModel, ExpressionSyntax eventSelector, out ITypeSymbol? eventType)
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

    private static ITypeSymbol[] GetEventParameterTypes(ITypeSymbol eventType)
    {
        // For delegates like Action<T>, we need to get the generic type arguments
        if (eventType is INamedTypeSymbol namedType)
        {
            // Handle Action (no parameters)
            if (string.Equals(namedType.Name, "Action", StringComparison.Ordinal) && namedType.TypeArguments.Length == 0)
            {
                return Array.Empty<ITypeSymbol>();
            }

            // Handle Action<T1, T2, ...>
            if (string.Equals(namedType.Name, "Action", StringComparison.Ordinal) && namedType.TypeArguments.Length > 0)
            {
                return namedType.TypeArguments.ToArray();
            }

            // Handle EventHandler<T> - expects single argument of type T (not the sender/args pattern)
            if (string.Equals(namedType.Name, "EventHandler", StringComparison.Ordinal) && namedType.TypeArguments.Length == 1)
            {
                return new[] { namedType.TypeArguments[0] };
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

    private static bool HasConversion(SemanticModel semanticModel, ITypeSymbol source, ITypeSymbol destination)
    {
        Conversion conversion = semanticModel.Compilation.ClassifyConversion(source, destination);
        return conversion.Exists && (conversion.IsImplicit || conversion.IsExplicit || conversion.IsIdentity);
    }
}
