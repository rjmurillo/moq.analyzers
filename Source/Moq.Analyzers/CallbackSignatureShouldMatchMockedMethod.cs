using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CallbackSignatureShouldMatchMockedMethod : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Moq1001";

        private static DiagnosticDescriptor NoMatchingMethodRule = new DiagnosticDescriptor(DiagnosticId,
            "Moq: No matching method", "No mocked methods with this signature.", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private const string Category = "Moq";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(NoMatchingMethodRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(VerifyCallbackLambdaSignature, SyntaxKind.InvocationExpression);
        }

        private static void VerifyCallbackLambdaSignature(SyntaxNodeAnalysisContext context)
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
            var mockedMethodInvocation = Helpers.FindMockedMethodInvocationFromSetupMethod(context.SemanticModel, setupInvocation);
            if (mockedMethodInvocation == null) return;

            var mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

            if (mockedMethodArguments.Count != lambdaParameters.Count)
            {
                var diagnostic = Diagnostic.Create(NoMatchingMethodRule, callbackLambda.ParameterList.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                for (int i = 0; i < mockedMethodArguments.Count; i++)
                {
                    var mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[i].Expression);
                    var lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameters[i].Type);
                    string mockedMethodTypeName = mockedMethodArgumentType.ConvertedType.ToString();
                    string lambdaParameterTypeName = lambdaParameterType.ConvertedType.ToString();
                    if (mockedMethodTypeName != lambdaParameterTypeName)
                    {
                        var diagnostic = Diagnostic.Create(NoMatchingMethodRule, callbackLambda.ParameterList.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
