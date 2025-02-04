namespace Moq.Analyzers;

/// <summary>
/// Setup of async method should use ReturnsAsync instead of .Result.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupShouldNotIncludeAsyncResultAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid setup parameter";
    private static readonly LocalizableString Message = "Setup of async methods should use ReturnsAsync instead of .Result";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AsyncUsesReturnsAsyncInsteadOfResult,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.AsyncUsesReturnsAsyncInsteadOfResult}.md");

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
        // Check Moq version and skip analysis if the version is 4.16.0 or later
        AssemblyIdentity? moqAssembly = context.Compilation.ReferencedAssemblyNames.FirstOrDefault(a => a.Name.Equals("Moq", StringComparison.OrdinalIgnoreCase));

        if (moqAssembly != null && moqAssembly.Version >= new Version(4, 16, 0))
        {
            // Skip analysis for Moq 4.16.0 or later
            return;
        }

        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);

        InvocationExpressionSyntax setupInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupInvocation.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return;
        }

        SymbolInfo memberAccessSymbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessExpression, context.CancellationToken);
        if (memberAccessSymbolInfo.Symbol is null || !memberAccessSymbolInfo.Symbol.IsMoqSetupMethod(knownSymbols))
        {
            return;
        }

        ExpressionSyntax? mockedMemberExpression = setupInvocation.FindMockedMemberExpressionFromSetupMethod();
        if (mockedMemberExpression == null)
        {
            return;
        }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(mockedMemberExpression, context.CancellationToken);
        if (symbolInfo.Symbol is not (IPropertySymbol or IMethodSymbol)
            || symbolInfo.Symbol.IsOverridable()
            || !symbolInfo.Symbol.IsTaskOrValueResultProperty(knownSymbols))
        {
            return;
        }

        Diagnostic diagnostic = mockedMemberExpression.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }
}
