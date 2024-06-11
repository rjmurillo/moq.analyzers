namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer : DiagnosticAnalyzer
{
    private static readonly MoqMethodDescriptorBase MoqAsMethodDescriptor = new MoqAsMethodDescriptor();

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
        if (context.Node is not InvocationExpressionSyntax invocationExpression)
        {
            return;
        }

        if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessSyntax)
        {
            return;
        }

        if (!MoqAsMethodDescriptor.IsMatch(context.SemanticModel, memberAccessSyntax, context.CancellationToken))
        {
            return;
        }

        if (!memberAccessSyntax.Name.TryGetGenericArguments(out SeparatedSyntaxList<TypeSyntax> typeArguments))
        {
            return;
        }

        if (typeArguments.Count != 1)
        {
            return;
        }

        TypeSyntax typeArgument = typeArguments[0];
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(typeArgument, context.CancellationToken);

        if (symbolInfo.Symbol is ITypeSymbol { TypeKind: not TypeKind.Interface })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, typeArgument.GetLocation()));
        }
    }
}
