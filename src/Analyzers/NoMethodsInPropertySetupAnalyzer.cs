namespace Moq.Analyzers;

/// <summary>
/// SetupGet/SetupSet/SetupProperty should be used for properties, not for methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoMethodsInPropertySetupAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Property setup used for a method";
    private static readonly LocalizableString Message = "SetupGet/SetupSet/SetupProperty should be used for properties, not for methods like '{0}'";
    private static readonly LocalizableString Description = "SetupGet/SetupSet/SetupProperty should be used for properties, not for methods.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PropertySetupUsedForMethod,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
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

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax setupGetOrSetInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupGetOrSetInvocation.Expression is not MemberAccessExpressionSyntax setupGetOrSetMethod) return;
        if (!string.Equals(setupGetOrSetMethod.Name.ToFullString(), "SetupGet", StringComparison.Ordinal)
            && !string.Equals(setupGetOrSetMethod.Name.ToFullString(), "SetupSet", StringComparison.Ordinal)
            && !string.Equals(setupGetOrSetMethod.Name.ToFullString(), "SetupProperty", StringComparison.Ordinal)) return;

        InvocationExpressionSyntax? mockedMethodCall = setupGetOrSetInvocation.FindMockedMethodInvocationFromSetupMethod();
        if (mockedMethodCall == null) return;

        ISymbol? mockedMethodSymbol = context.SemanticModel.GetSymbolInfo(mockedMethodCall, context.CancellationToken).Symbol;
        if (mockedMethodSymbol == null) return;

        Diagnostic diagnostic = mockedMethodCall.CreateDiagnostic(Rule, mockedMethodSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
