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
/// The shared argument-validation tail lives in EventSyntaxExtensions.AnalyzeEventArgumentsAgainstEventSignature.
///
/// Both analyzers serve critical roles in preventing runtime exceptions by validating
/// event argument types at compile time, but they target different Moq usage patterns.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RaiseEventArgumentsShouldMatchEventSignatureAnalyzer : MoqDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Raise event arguments should match event signature";
    private static readonly LocalizableString Message = "Raise event arguments should match the '{0}' event delegate signature";
    private static readonly LocalizableString Description = "Raise event arguments should match the event delegate signature.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature,
        Title,
        Message,
        DiagnosticCategory.Correctness,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.RaiseEventArgumentsShouldMatchEventSignature}.md");

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

        // Check if this is a Raise method call on a Mock<T>
        if (!context.SemanticModel.IsRaiseInvocation(invocation, knownSymbols, context.CancellationToken))
        {
            return;
        }

        context.AnalyzeEventArgumentsAgainstEventSignature(invocation, knownSymbols, Rule);
    }
}
