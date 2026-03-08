namespace Moq.Analyzers;

/// <summary>
/// Analyzer for the Setup.Raises() method - validates event arguments match the delegate signature.
///
/// IMPORTANT FOR MAINTAINERS:
/// This analyzer handles the setup-chained event triggering pattern:
/// mock.Setup(x => x.Method()).Raises(x => x.Event += null, args...)
///
/// This is different from RaiseEventArgumentsShouldMatchEventSignatureAnalyzer which handles
/// the direct pattern: mock.Raise(x => x.Event += null, args...)
///
/// Key architectural differences:
/// 1. This analyzes Raises() method calls that are chained after Setup() calls
/// 2. Uses symbol-based detection via SemanticModel.IsRaisesInvocation for robust identification
/// 3. Implements setup-based event triggering validation (not immediate)
///
///
/// Both this analyzer and RaiseEventArgumentsShouldMatchEventSignatureAnalyzer are essential
/// for comprehensive event validation coverage across all Moq event patterns, preventing
/// subtle runtime failures by catching type mismatches at compile time.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RaisesEventArgumentsShouldMatchEventSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Raises event arguments should match event signature";
    private static readonly LocalizableString Message = "Raises event arguments should match the '{0}' event delegate signature";
    private static readonly LocalizableString Description = "Raises event arguments should match the event delegate signature.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature}.md");

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

        if (!context.SemanticModel.IsRaisesInvocation(invocation, knownSymbols))
        {
            return;
        }

        if (!TryGetRaisesMethodArguments(invocation, context.SemanticModel, knownSymbols, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
        {
            return;
        }

        string eventName = GetEventNameFromSelector(invocation, context.SemanticModel);

        context.ValidateEventArgumentTypes(eventArguments, expectedParameterTypes, invocation, Rule, eventName ?? "event");
    }

    private static bool TryGetRaisesMethodArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, KnownSymbols knownSymbols, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes)
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

    private static string GetEventNameFromSelector(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 1)
        {
            return "event";
        }

        ExpressionSyntax eventSelector = arguments[0].Expression;

        return semanticModel.TryGetEventNameFromLambdaSelector(eventSelector, out string? eventName)
            ? eventName!
            : "event";
    }
}
