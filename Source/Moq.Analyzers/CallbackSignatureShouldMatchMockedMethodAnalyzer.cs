using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        InvocationExpressionSyntax? callbackOrReturnsInvocation = (InvocationExpressionSyntax)context.Node;

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

        InvocationExpressionSyntax? setupInvocation = Helpers.FindSetupMethodFromCallbackInvocation(context.SemanticModel, callbackOrReturnsInvocation);
        InvocationExpressionSyntax? mockedMethodInvocation = Helpers.FindMockedMethodInvocationFromSetupMethod(setupInvocation);
        if (mockedMethodInvocation == null) return;

        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

        if (mockedMethodArguments.Count != lambdaParameters.Count)
        {
            Diagnostic? diagnostic = Diagnostic.Create(Rule, callbackLambda.ParameterList.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            for (int i = 0; i < mockedMethodArguments.Count; i++)
            {
                TypeInfo mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[i].Expression);
                TypeInfo lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameters[i].Type);
                string mockedMethodTypeName = mockedMethodArgumentType.ConvertedType.ToString();
                string lambdaParameterTypeName = lambdaParameterType.ConvertedType.ToString();
                if (mockedMethodTypeName != lambdaParameterTypeName)
                {
                    Diagnostic? diagnostic = Diagnostic.Create(Rule, callbackLambda.ParameterList.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
