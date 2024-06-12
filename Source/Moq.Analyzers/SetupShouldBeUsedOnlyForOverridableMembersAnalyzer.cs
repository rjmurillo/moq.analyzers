namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        Diagnostics.SetupShouldBeUsedOnlyForOverridableMembersId,
        Diagnostics.SetupShouldBeUsedOnlyForOverridableMembersTitle,
        Diagnostics.SetupShouldBeUsedOnlyForOverridableMembersMessage,
        Diagnostics.Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax? setupInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupInvocation.Expression is MemberAccessExpressionSyntax memberAccessExpression && Helpers.IsMoqSetupMethod(context.SemanticModel, memberAccessExpression, context.CancellationToken))
        {
            ExpressionSyntax? mockedMemberExpression = Helpers.FindMockedMemberExpressionFromSetupMethod(setupInvocation);
            if (mockedMemberExpression == null)
            {
                return;
            }

            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(mockedMemberExpression, context.CancellationToken);
            if (symbolInfo.Symbol is IPropertySymbol or IMethodSymbol
                && !IsMethodOverridable(symbolInfo.Symbol))
            {
                Diagnostic? diagnostic = Diagnostic.Create(Rule, mockedMemberExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsMethodOverridable(ISymbol methodSymbol)
    {
        return !methodSymbol.IsSealed && (methodSymbol.IsVirtual || methodSymbol.IsAbstract || methodSymbol.IsOverride);
    }
}
