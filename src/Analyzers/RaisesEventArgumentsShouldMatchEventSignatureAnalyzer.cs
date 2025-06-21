#pragma warning disable CS0436 // Type conflicts with imported type

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
/// The #pragma warning disable CS0436 above is necessary because this analyzer may encounter
/// type conflicts with imported types from shared dependencies or other analyzers in the
/// compilation context. This is a common issue when multiple analyzers reference similar
/// type definitions.
///
/// Both this analyzer and RaiseEventArgumentsShouldMatchEventSignatureAnalyzer are essential
/// for comprehensive event validation coverage across all Moq event patterns, preventing
/// subtle runtime failures by catching type mismatches at compile time.
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

        // Check if this is a Raises method call using symbol-based detection
        if (!context.SemanticModel.IsRaisesInvocation(invocation) && !invocation.IsRaisesMethodCall())
        {
            return;
        }

        if (!TryGetRaisesMethodArguments(invocation, context.SemanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
        {
            return;
        }

        EventSyntaxExtensions.ValidateEventArgumentTypes(context, eventArguments, expectedParameterTypes, invocation, Rule);
    }

    private static bool TryGetRaisesMethodArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes)
    {
        return EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            semanticModel,
            out eventArguments,
            out expectedParameterTypes,
            (sm, selector) =>
            {
                bool success = EventSyntaxExtensions.TryGetEventTypeFromLambdaSelector(sm, selector, out ITypeSymbol? eventType);
                return (success, eventType);
            });
    }
}
