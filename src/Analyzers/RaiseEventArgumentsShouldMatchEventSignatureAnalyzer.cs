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
        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a Raise method call on a Mock<T>
        if (!IsRaiseMethodCall(context.SemanticModel, invocation, knownSymbols))
        {
            return;
        }

        if (!TryGetRaiseMethodArguments(invocation, context.SemanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
        {
            return;
        }

        ValidateArgumentTypes(context, eventArguments, expectedParameterTypes, invocation);
    }

    private static bool TryGetRaiseMethodArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes)
    {
        eventArguments = Array.Empty<ArgumentSyntax>();
        expectedParameterTypes = Array.Empty<ITypeSymbol>();

        // Get the arguments to the Raise method
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // Raise method should have at least 1 argument (the event selector)
        if (arguments.Count < 1)
        {
            return false;
        }

        // First argument should be a lambda that selects the event
        ExpressionSyntax eventSelector = arguments[0].Expression;
        if (!TryGetEventTypeFromSelector(semanticModel, eventSelector, out ITypeSymbol? eventType))
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

    private static void ValidateArgumentTypes(SyntaxNodeAnalysisContext context, ArgumentSyntax[] eventArguments, ITypeSymbol[] expectedParameterTypes, InvocationExpressionSyntax invocation)
    {
        if (eventArguments.Length != expectedParameterTypes.Length)
        {
            // Wrong number of arguments
            Location location;
            if (eventArguments.Length < expectedParameterTypes.Length)
            {
                // Too few arguments: report at the start of the statement
                ExpressionStatementSyntax? statement = invocation.Parent as ExpressionStatementSyntax;
                if (statement != null)
                {
                    location = statement.GetFirstToken().GetLocation();
                }
                else
                {
                    location = invocation.GetLocation();
                }
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
        Conversion conversion = semanticModel.Compilation.ClassifyConversion(source, destination);
        return conversion.Exists && (conversion.IsImplicit || conversion.IsExplicit || conversion.IsIdentity);
    }
}
