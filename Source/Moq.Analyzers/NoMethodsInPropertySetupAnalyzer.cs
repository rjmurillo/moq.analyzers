namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoMethodsInPropertySetupAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.NoMethodsInPropertySetupId,
        Diagnostics.NoMethodsInPropertySetupTitle,
        Diagnostics.NoMethodsInPropertySetupMessage,
        Diagnostics.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{Diagnostics.NoMethodsInPropertySetupId}.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax? setupGetOrSetInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupGetOrSetInvocation.Expression is not MemberAccessExpressionSyntax setupGetOrSetMethod) return;
        if (!string.Equals(setupGetOrSetMethod.Name.ToFullString(), "SetupGet", StringComparison.Ordinal)
            && !string.Equals(setupGetOrSetMethod.Name.ToFullString(), "SetupSet", StringComparison.Ordinal)) return;

        InvocationExpressionSyntax? mockedMethodCall = Helpers.FindMockedMethodInvocationFromSetupMethod(setupGetOrSetInvocation);
        if (mockedMethodCall == null) return;

        ISymbol? mockedMethodSymbol = context.SemanticModel.GetSymbolInfo(mockedMethodCall, context.CancellationToken).Symbol;
        if (mockedMethodSymbol == null) return;

        Diagnostic? diagnostic = Diagnostic.Create(Rule, mockedMethodCall.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
