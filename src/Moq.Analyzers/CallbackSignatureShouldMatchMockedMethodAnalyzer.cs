namespace Moq.Analyzers;

/// <summary>
/// Callback signature must match the signature of the mocked method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CallbackSignatureShouldMatchMockedMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Bad callback parameters";
    private static readonly LocalizableString Message = "Callback signature must match the signature of the mocked method";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.BadCallbackParameters,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.BadCallbackParameters}.md");

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
        InvocationExpressionSyntax callbackOrReturnsInvocation = (InvocationExpressionSyntax)context.Node;

        SeparatedSyntaxList<ArgumentSyntax> callbackOrReturnsMethodArguments = callbackOrReturnsInvocation.ArgumentList.Arguments;

        // Ignoring Callback() and Return() calls without lambda arguments
        if (callbackOrReturnsMethodArguments.Count == 0) return;

        if (!context.SemanticModel.IsCallbackOrReturnInvocation(callbackOrReturnsInvocation)) return;

        ParenthesizedLambdaExpressionSyntax? callbackLambda = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;

        // Ignoring callbacks without lambda
        if (callbackLambda == null) return;

        // Ignoring calls with no arguments because those are valid in Moq
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters = callbackLambda.ParameterList.Parameters;
        if (lambdaParameters.Count == 0) return;

        InvocationExpressionSyntax? setupInvocation = context.SemanticModel.FindSetupMethodFromCallbackInvocation(callbackOrReturnsInvocation, context.CancellationToken);
        InvocationExpressionSyntax? mockedMethodInvocation = setupInvocation.FindMockedMethodInvocationFromSetupMethod();
        if (mockedMethodInvocation == null) return;

        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

        if (mockedMethodArguments.Count != lambdaParameters.Count)
        {
            Diagnostic diagnostic = Diagnostic.Create(Rule, callbackLambda.ParameterList.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            for (int argumentIndex = 0; argumentIndex < mockedMethodArguments.Count; argumentIndex++)
            {
                TypeSyntax? lambdaParameterTypeSyntax = lambdaParameters[argumentIndex].Type;

                // TODO: Don't know if continue or break is the right thing to do here
                if (lambdaParameterTypeSyntax is null) continue;

                TypeInfo lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameterTypeSyntax, context.CancellationToken);

                TypeInfo mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[argumentIndex].Expression, context.CancellationToken);



                // Check if types are assignable rather than strictly equal
                ITypeSymbol? lambdaParameterTypeSymbol = lambdaParameterType.Type;
                ITypeSymbol? mockedMethodTypeSymbol = mockedMethodArgumentType.Type;

                if (lambdaParameterTypeSymbol is not null && mockedMethodTypeSymbol is not null)
                {

                    if (!context.SemanticModel.Compilation.ClassifyConversion(lambdaParameterTypeSymbol, mockedMethodTypeSymbol).IsImplicit)
                    {

                        Diagnostic diagnostic = lambdaParameters[argumentIndex].GetLocation().CreateDiagnostic(Rule);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                else
                {
                    string? mockedMethodTypeName = mockedMethodArgumentType.ConvertedType?.ToString();
                    string? lambdaParameterTypeName = lambdaParameterType.ConvertedType?.ToString();

                    if (!string.Equals(mockedMethodTypeName, lambdaParameterTypeName, StringComparison.Ordinal))
                    {
                        Diagnostic diagnostic = callbackLambda.ParameterList.CreateDiagnostic(Rule);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
