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
public class RaisesEventArgumentsShouldMatchEventSignatureAnalyzer : MoqDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Raises event arguments should match event signature";
    private static readonly LocalizableString Message = "Raises event arguments should match the '{0}' event delegate signature";
    private static readonly LocalizableString Description = "Raises event arguments should match the event delegate signature.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature,
        Title,
        Message,
        DiagnosticCategory.Correctness,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    private protected override void RegisterCompilationActions(CompilationStartAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        context.RegisterSyntaxNodeAction(
            syntaxNodeContext => Analyze(syntaxNodeContext, knownSymbols),
            SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        if (!context.SemanticModel.IsRaisesInvocation(invocation, knownSymbols, context.CancellationToken))
        {
            return;
        }

        if (!EventSyntaxExtensions.TryGetEventMethodArgumentsFromLambdaSelector(invocation, context.SemanticModel, knownSymbols, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes, out bool senderCanBeOmitted, context.CancellationToken))
        {
            return;
        }

        string eventName = EventSyntaxExtensions.GetEventNameFromSelector(invocation, context.SemanticModel, context.CancellationToken);

        context.ValidateEventArgumentTypes(eventArguments, expectedParameterTypes, senderCanBeOmitted, knownSymbols.EventArgs, invocation, Rule, eventName);
    }
}
