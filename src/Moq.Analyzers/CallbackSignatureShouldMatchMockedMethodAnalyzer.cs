using System.Diagnostics;

namespace Moq.Analyzers;

/// <summary>
/// Callback signature must match the signature of the mocked method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CallbackSignatureShouldMatchMockedMethodAnalyzer : DiagnosticAnalyzer
{
    internal const string RuleId = "Moq1100";
    private const string Title = "Moq: Bad callback parameters";
    private const string Message = "Callback signature must match the signature of the mocked method";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{RuleId}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

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

        if (!Helpers.IsCallbackOrReturnInvocation(context.SemanticModel, callbackOrReturnsInvocation)) return;

        ParenthesizedLambdaExpressionSyntax? callbackLambda = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;

        // Ignoring callbacks without lambda
        if (callbackLambda == null) return;

        // Ignoring calls with no arguments because those are valid in Moq
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters = callbackLambda.ParameterList.Parameters;
        if (lambdaParameters.Count == 0) return;

        InvocationExpressionSyntax? setupInvocation = Helpers.FindSetupMethodFromCallbackInvocation(context.SemanticModel, callbackOrReturnsInvocation, context.CancellationToken);
        InvocationExpressionSyntax? mockedMethodInvocation = Helpers.FindMockedMethodInvocationFromSetupMethod(setupInvocation);
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
                Debug.Assert(lambdaParameterTypeSyntax != null, nameof(lambdaParameterTypeSyntax) + " != null");

                // TODO: Don't know if continue or break is the right thing to do here
#pragma warning disable S2589 // Boolean expressions should not be gratuitous
                if (lambdaParameterTypeSyntax is null) continue;
#pragma warning restore S2589 // Boolean expressions should not be gratuitous

                TypeInfo lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameterTypeSyntax, context.CancellationToken);

                TypeInfo mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[argumentIndex].Expression, context.CancellationToken);

                string? mockedMethodTypeName = mockedMethodArgumentType.ConvertedType?.ToString();
                string? lambdaParameterTypeName = lambdaParameterType.ConvertedType?.ToString();

                if (!string.Equals(mockedMethodTypeName, lambdaParameterTypeName, StringComparison.Ordinal))
                {
                    Diagnostic diagnostic = Diagnostic.Create(Rule, callbackLambda.ParameterList.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
