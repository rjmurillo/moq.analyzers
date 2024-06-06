using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CallbackSignatureShouldMatchMockedMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        Diagnostics.CallbackSignatureShouldMatchMockedMethodId,
        Diagnostics.CallbackSignatureShouldMatchMockedMethodTitle,
        Diagnostics.CallbackSignatureShouldMatchMockedMethodMessage,
        Diagnostics.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var callbackOrReturnsInvocation = (InvocationExpressionSyntax)context.Node;

        var callbackOrReturnsMethodArguments = callbackOrReturnsInvocation.ArgumentList.Arguments;

        // Ignoring Callback() and Return() calls without lambda arguments
        if (callbackOrReturnsMethodArguments.Count == 0) return;

        if (!Helpers.IsCallbackOrReturnInvocation(context.SemanticModel, callbackOrReturnsInvocation)) return;

        var callbackLambda = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;

        // Ignoring callbacks without lambda
        if (callbackLambda == null) return;

        // Ignoring calls with no arguments because those are valid in Moq
        var lambdaParameters = callbackLambda.ParameterList.Parameters;
        if (lambdaParameters.Count == 0) return;

        var setupInvocation = Helpers.FindSetupMethodFromCallbackInvocation(context.SemanticModel, callbackOrReturnsInvocation);
        var mockedMethodInvocation = Helpers.FindMockedMethodInvocationFromSetupMethod(setupInvocation);
        if (mockedMethodInvocation == null) return;

        var mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

        if (mockedMethodArguments.Count != lambdaParameters.Count)
        {
            var diagnostic = Diagnostic.Create(Rule, callbackLambda.ParameterList.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            for (int i = 0; i < mockedMethodArguments.Count; i++)
            {
                var mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[i].Expression, context.CancellationToken);
                var lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameters[i].Type, context.CancellationToken);
                string mockedMethodTypeName = mockedMethodArgumentType.ConvertedType.ToString();
                string lambdaParameterTypeName = lambdaParameterType.ConvertedType.ToString();
                if (mockedMethodTypeName != lambdaParameterTypeName)
                {
                    var diagnostic = Diagnostic.Create(Rule, callbackLambda.ParameterList.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
