namespace Moq.Analyzers;

/// <summary>
/// Mock.As() should take interfaces only.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer : DiagnosticAnalyzer
{
    internal const string RuleId = "Moq1300";
    private const string Title = "Moq: Invalid As type parameter";
    private const string Message = "Mock.As() should take interfaces only";

    private static readonly MoqMethodDescriptorBase MoqAsMethodDescriptor = new MoqAsMethodDescriptor();

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{RuleId}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
