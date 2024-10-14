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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax setupInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupInvocation.Expression is not MemberAccessExpressionSyntax memberAccessExpression ||
            !context.SemanticModel.IsMoqSetupMethod(memberAccessExpression, context.CancellationToken))
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
            || !symbolInfo.Symbol.IsMethodReturnTypeTask())
        {
            return;
        }

        Diagnostic diagnostic = mockedMemberExpression.GetLocation().CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }
}
