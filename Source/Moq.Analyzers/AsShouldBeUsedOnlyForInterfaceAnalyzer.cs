namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        Diagnostics.AsShouldBeUsedOnlyForInterfaceId,
        Diagnostics.AsShouldBeUsedOnlyForInterfaceTitle,
        Diagnostics.AsShouldBeUsedOnlyForInterfaceMessage,
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
        var asInvocation = (InvocationExpressionSyntax)context.Node;

        if (asInvocation.Expression is MemberAccessExpressionSyntax memberAccessExpression
            && Helpers.IsMoqAsMethod(context.SemanticModel, memberAccessExpression)
            && memberAccessExpression.Name is GenericNameSyntax genericName
            && genericName.TypeArgumentList.Arguments.Count == 1)
        {
            var typeArgument = genericName.TypeArgumentList.Arguments[0];
            var symbolInfo = context.SemanticModel.GetSymbolInfo(typeArgument, context.CancellationToken);
            if (symbolInfo.Symbol is ITypeSymbol typeSymbol && typeSymbol.TypeKind != TypeKind.Interface)
            {
                var diagnostic = Diagnostic.Create(Rule, typeArgument.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
