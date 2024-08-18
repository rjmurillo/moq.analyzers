namespace Moq.Analyzers;

/// <summary>
/// SetupGet/SetupSet should be used for properties, not for methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoMethodsInPropertySetupAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Property setup used for a method";
    private static readonly LocalizableString Message = "SetupGet/SetupSet should be used for properties, not for methods";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PropertySetupUsedForMethod,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.PropertySetupUsedForMethod}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax setupGetOrSetInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupGetOrSetInvocation.Expression is not MemberAccessExpressionSyntax setupGetOrSetMethod) return;
        if (!string.Equals(setupGetOrSetMethod.Name.ToFullString(), "SetupGet", StringComparison.Ordinal)
            && !string.Equals(setupGetOrSetMethod.Name.ToFullString(), "SetupSet", StringComparison.Ordinal)) return;

        InvocationExpressionSyntax? mockedMethodCall = setupGetOrSetInvocation.FindMockedMethodInvocationFromSetupMethod();
        if (mockedMethodCall == null) return;

        ISymbol? mockedMethodSymbol = context.SemanticModel.GetSymbolInfo(mockedMethodCall, context.CancellationToken).Symbol;
        if (mockedMethodSymbol == null) return;

        Diagnostic diagnostic = mockedMethodCall.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }
}
