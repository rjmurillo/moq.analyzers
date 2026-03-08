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

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        context.RegisterSyntaxNodeAction(
            syntaxNodeContext => Analyze(syntaxNodeContext, knownSymbols),
            SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a Raise method call on a Mock<T>
        if (!IsRaiseMethodCall(context.SemanticModel, invocation, knownSymbols))
        {
            return;
        }

        if (!TryGetRaiseMethodArguments(invocation, context.SemanticModel, knownSymbols, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
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

        context.ValidateEventArgumentTypes(eventArguments, expectedParameterTypes, invocation, Rule, eventName ?? "event");
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
            static (sm, selector) =>
            {
                bool success = sm.TryGetEventTypeFromLambdaSelector(selector, out ITypeSymbol? eventType);
                return (success, eventType);
            },
            knownSymbols);
    }

    private static bool IsRaiseMethodCall(SemanticModel semanticModel, InvocationExpressionSyntax invocation, MoqKnownSymbols knownSymbols)
    {
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        return methodSymbol.IsInstanceOf(knownSymbols.Mock1Raise);
    }
}
