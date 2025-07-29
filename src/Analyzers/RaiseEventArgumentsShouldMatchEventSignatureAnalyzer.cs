namespace Moq.Analyzers;

/// <summary>
/// Analyzer for the Mock.Raise() method - validates event arguments match the delegate signature.
///
/// IMPORTANT FOR MAINTAINERS:
/// This analyzer handles the direct event triggering pattern: mock.Raise(x => x.Event += null, args...)
/// This is different from RaisesEventArgumentsShouldMatchEventSignatureAnalyzer which handles
/// the setup-chained pattern: mock.Setup(x => x.Method()).Raises(x => x.Event += null, args...)
///
/// Key differences from the similar RaisesEventArgumentsShouldMatchEventSignatureAnalyzer:
/// 1. This analyzes direct Mock.Raise() calls on the mock object
/// 2. Uses proper symbol analysis via MoqKnownSymbols.Mock1Raise for robust detection
/// 3. Implements immediate event triggering validation (not setup-based)
///
/// Both analyzers serve critical roles in preventing runtime exceptions by validating
/// event argument types at compile time, but they target different Moq usage patterns.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RaiseEventArgumentsShouldMatchEventSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Raise event arguments should match event signature";
    private static readonly LocalizableString Message = "Raise event arguments should match the '{0}' event delegate signature";
    private static readonly LocalizableString Description = "Raise event arguments should match the event delegate signature.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
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

        KnownSymbols wellKnownSymbols = new(context.SemanticModel.Compilation);

        if (!TryGetRaiseMethodArguments(invocation, context.SemanticModel, wellKnownSymbols, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
        {
            return;
        }

        // Extract event name from the first argument (event selector lambda)
        string? eventName = null;
        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            ExpressionSyntax eventSelector = invocation.ArgumentList.Arguments[0].Expression;
            context.SemanticModel.TryGetEventNameFromLambdaSelector(eventSelector, out eventName);
        }

        ValidateArgumentTypesWithEventName(context, eventArguments, expectedParameterTypes, invocation, eventName ?? "event");
    }

    private static bool TryGetRaiseMethodArguments(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        KnownSymbols knownSymbols,
        out ArgumentSyntax[] eventArguments,
        out ITypeSymbol[] expectedParameterTypes)
    {
        return EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            semanticModel,
            out eventArguments,
            out expectedParameterTypes,
            (sm, selector) =>
            {
                bool success = sm.TryGetEventTypeFromLambdaSelector(selector, out ITypeSymbol? eventType);
                return (success, eventType);
            },
            knownSymbols);
    }

    private static void ValidateArgumentTypesWithEventName(SyntaxNodeAnalysisContext context, ArgumentSyntax[] eventArguments, ITypeSymbol[] expectedParameterTypes, InvocationExpressionSyntax invocation, string eventName)
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

            Diagnostic diagnostic = location.CreateDiagnostic(Rule, eventName);
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
                Diagnostic diagnostic = eventArguments[i].GetLocation().CreateDiagnostic(Rule, eventName);
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
}
