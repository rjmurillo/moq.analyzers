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
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);

        // Check if this is a Raises method call using symbol-based detection
        if (!context.SemanticModel.IsRaisesInvocation(invocation, knownSymbols) && !invocation.IsRaisesMethodCall(context.SemanticModel, knownSymbols))
        {
            return;
        }

        if (!TryGetRaisesMethodArguments(invocation, context.SemanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
        {
            return;
        }

        // Extract event name from the lambda selector (first argument)
        string eventName = TryGetEventNameFromLambdaSelector(invocation, context.SemanticModel) ?? "event";

        EventSyntaxExtensions.ValidateEventArgumentTypes(context, eventArguments, expectedParameterTypes, invocation, Rule, eventName);
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
                bool success = sm.TryGetEventTypeFromLambdaSelector(selector, out ITypeSymbol? eventType);
                return (success, eventType);
            });
    }

    /// <summary>
    /// Extracts the event name from a lambda selector of the form: x => x.EventName += null.
    /// </summary>
    /// <param name="invocation">The method invocation containing the lambda selector.</param>
    /// <param name="semanticModel">The semantic model.</param>
    /// <returns>The event name if found; otherwise null.</returns>
    private static string? TryGetEventNameFromLambdaSelector(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        // Get the first argument which should be the lambda selector
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 1)
        {
            return null;
        }

        ExpressionSyntax eventSelector = arguments[0].Expression;

        // The event selector should be a lambda like: p => p.EventName += null
        if (eventSelector is not LambdaExpressionSyntax lambda)
        {
            return null;
        }

        // The body should be an assignment expression with += operator
        if (lambda.Body is not AssignmentExpressionSyntax assignment ||
            !assignment.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken))
        {
            return null;
        }

        // The left side should be a member access to the event
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        // Get the symbol for the event
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is IEventSymbol eventSymbol)
        {
            return eventSymbol.Name;
        }

        return null;
    }
}
