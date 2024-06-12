﻿namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupShouldNotIncludeAsyncResultAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.SetupShouldNotIncludeAsyncResultId,
        Diagnostics.SetupShouldNotIncludeAsyncResultTitle,
        Diagnostics.SetupShouldNotIncludeAsyncResultMessage,
        Diagnostics.Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{Diagnostics.SetupShouldNotIncludeAsyncResultId}.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax? setupInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupInvocation.Expression is MemberAccessExpressionSyntax memberAccessExpression && Helpers.IsMoqSetupMethod(context.SemanticModel, memberAccessExpression, context.CancellationToken))
        {
            ExpressionSyntax? mockedMemberExpression = Helpers.FindMockedMemberExpressionFromSetupMethod(setupInvocation);
            if (mockedMemberExpression == null)
            {
                return;
            }

            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(mockedMemberExpression, context.CancellationToken);
            if ((symbolInfo.Symbol is IPropertySymbol || symbolInfo.Symbol is IMethodSymbol)
                && !IsMethodOverridable(symbolInfo.Symbol)
                && IsMethodReturnTypeTask(symbolInfo.Symbol))
            {
                Diagnostic? diagnostic = Diagnostic.Create(Rule, mockedMemberExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsMethodOverridable(ISymbol methodSymbol)
    {
        return !methodSymbol.IsSealed
               && (methodSymbol.IsVirtual || methodSymbol.IsAbstract || methodSymbol.IsOverride);
    }

    private static bool IsMethodReturnTypeTask(ISymbol methodSymbol)
    {
        string? type = methodSymbol.ToDisplayString();
        return string.Equals(type, "System.Threading.Tasks.Task", StringComparison.Ordinal)
               || string.Equals(type, "System.Threading.ValueTask", StringComparison.Ordinal)
               || type.StartsWith("System.Threading.Tasks.Task<", StringComparison.Ordinal)
               || (type.StartsWith("System.Threading.Tasks.ValueTask<", StringComparison.Ordinal)
                   && type.EndsWith(".Result", StringComparison.Ordinal));
    }
}
