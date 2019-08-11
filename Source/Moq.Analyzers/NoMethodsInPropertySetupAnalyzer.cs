namespace Moq.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoMethodsInPropertySetupAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Diagnostics.NoMethodsInPropertySetupId,
            Diagnostics.NoMethodsInPropertySetupTitle,
            Diagnostics.NoMethodsInPropertySetupMessage,
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
            var setupGetOrSetInvocation = (InvocationExpressionSyntax)context.Node;

            var setupGetOrSetMethod = setupGetOrSetInvocation.Expression as MemberAccessExpressionSyntax;
            if (setupGetOrSetMethod == null) return;
            if (setupGetOrSetMethod.Name.ToFullString() != "SetupGet" && setupGetOrSetMethod.Name.ToFullString() != "SetupSet") return;

            var mockedMethodCall = Helpers.FindMockedMethodInvocationFromSetupMethod(setupGetOrSetInvocation);
            if (mockedMethodCall == null) return;

            var mockedMethodSymbol = context.SemanticModel.GetSymbolInfo(mockedMethodCall).Symbol;
            if (mockedMethodSymbol == null) return;

            var diagnostic = Diagnostic.Create(Rule, mockedMethodCall.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
