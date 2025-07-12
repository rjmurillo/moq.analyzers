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

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);

        InvocationExpressionSyntax callbackOrReturnsInvocation = (InvocationExpressionSyntax)context.Node;

        SeparatedSyntaxList<ArgumentSyntax> callbackOrReturnsMethodArguments = callbackOrReturnsInvocation.ArgumentList.Arguments;

        // Ignoring Callback() and Return() calls without lambda arguments
        if (callbackOrReturnsMethodArguments.Count == 0) return;

        if (!context.SemanticModel.IsCallbackOrReturnInvocation(callbackOrReturnsInvocation, knownSymbols)) return;

        ParenthesizedLambdaExpressionSyntax? callbackLambda = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;

        // Ignoring callbacks without lambda
        if (callbackLambda == null) return;

        // Ignoring calls with no arguments because those are valid in Moq
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters = callbackLambda.ParameterList.Parameters;
        if (lambdaParameters.Count == 0) return;

        InvocationExpressionSyntax? setupInvocation = context.SemanticModel.FindSetupMethodFromCallbackInvocation(knownSymbols, callbackOrReturnsInvocation, context.CancellationToken);
        InvocationExpressionSyntax? mockedMethodInvocation = setupInvocation.FindMockedMethodInvocationFromSetupMethod();
        if (mockedMethodInvocation == null) return;

        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

        if (mockedMethodArguments.Count != lambdaParameters.Count)
        {
            Diagnostic diagnostic = callbackLambda.ParameterList.CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            ValidateParameters(context, mockedMethodArguments, lambdaParameters);
        }
    }

    private static void ValidateParameters(
        SyntaxNodeAnalysisContext context,
        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments,
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters)
    {
        for (int argumentIndex = 0; argumentIndex < mockedMethodArguments.Count; argumentIndex++)
        {
            TypeSyntax? lambdaParameterTypeSyntax = lambdaParameters[argumentIndex].Type;

            // We're unable to get the type from the Syntax Tree, so abort the type checking because something else
            // is happening (e.g., we're compiling on partial code) and we need the type to do the additional checks.
            if (lambdaParameterTypeSyntax is null)
            {
                continue;
            }

            TypeInfo lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameterTypeSyntax, context.CancellationToken);
            TypeInfo mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[argumentIndex].Expression, context.CancellationToken);

            ITypeSymbol? lambdaParameterTypeSymbol = lambdaParameterType.Type;
            ITypeSymbol? mockedMethodTypeSymbol = mockedMethodArgumentType.Type;

            if (lambdaParameterTypeSymbol is null || mockedMethodTypeSymbol is null)
            {
                continue;
            }

            if (!context.SemanticModel.HasConversion(mockedMethodTypeSymbol, lambdaParameterTypeSymbol))
            {
                Diagnostic diagnostic = lambdaParameters[argumentIndex].CreateDiagnostic(Rule);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
