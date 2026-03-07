namespace Moq.Analyzers;

/// <summary>
/// Setup of async method should use ReturnsAsync instead of .Result.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupShouldNotIncludeAsyncResultAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid setup parameter";
    private static readonly LocalizableString Message = "Setup of async method '{0}' should use ReturnsAsync instead of .Result";
    private static readonly LocalizableString Description = "Setup of async methods should use ReturnsAsync instead of .Result.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AsyncUsesReturnsAsyncInsteadOfResult,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.AsyncUsesReturnsAsyncInsteadOfResult}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Check Moq version once per compilation, skip for 4.16.0 or later
        AssemblyIdentity? moqAssembly = context.Compilation.ReferencedAssemblyNames
            .FirstOrDefault(a => a.Name.Equals("Moq", StringComparison.OrdinalIgnoreCase));

        if (moqAssembly != null && moqAssembly.Version >= new Version(4, 16, 0))
        {
            return;
        }

        context.RegisterSyntaxNodeAction(
            syntaxNodeContext => Analyze(syntaxNodeContext, knownSymbols),
            SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
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

        string methodName = GetMethodName(mockedMemberExpression);
        Diagnostic diagnostic = mockedMemberExpression.CreateDiagnostic(Rule, methodName);
        context.ReportDiagnostic(diagnostic);
    }

    private static string GetMethodName(ExpressionSyntax mockedMemberExpression)
    {
        // Handle cases like c.GenericTaskAsync().Result
        // where mockedMemberExpression is the entire expression
        return mockedMemberExpression switch
        {
            MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax invocation } =>
                GetMethodNameFromInvocation(invocation),
            InvocationExpressionSyntax invocation =>
                GetMethodNameFromInvocation(invocation),
            MemberAccessExpressionSyntax memberAccess =>
                memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier =>
                identifier.Identifier.ValueText,
            _ => "Unknown",
        };
    }

    private static string GetMethodNameFromInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => "Unknown",
        };
    }
}
