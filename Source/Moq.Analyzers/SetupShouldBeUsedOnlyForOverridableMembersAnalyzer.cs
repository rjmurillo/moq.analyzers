using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        Diagnostics.SetupShouldBeUsedOnlyForOverridableMembersId,
        Diagnostics.SetupShouldBeUsedOnlyForOverridableMembersTitle,
        Diagnostics.SetupShouldBeUsedOnlyForOverridableMembersMessage,
        Diagnostics.Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var setupInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupInvocation.Expression is MemberAccessExpressionSyntax memberAccessExpression && Helpers.IsMoqSetupMethod(context.SemanticModel, memberAccessExpression))
        {
            var mockedMemberExpression = Helpers.FindMockedMemberExpressionFromSetupMethod(setupInvocation);
            if (mockedMemberExpression == null)
            {
                return;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(mockedMemberExpression, context.CancellationToken);
            if (symbolInfo.Symbol is IPropertySymbol || symbolInfo.Symbol is IMethodSymbol)
            {
                if (IsMethodOverridable(symbolInfo.Symbol) == false)
                {
                    var diagnostic = Diagnostic.Create(Rule, mockedMemberExpression.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool IsMethodOverridable(ISymbol methodSymbol)
    {
        return methodSymbol.IsSealed == false && (methodSymbol.IsVirtual || methodSymbol.IsAbstract || methodSymbol.IsOverride);
    }
}
