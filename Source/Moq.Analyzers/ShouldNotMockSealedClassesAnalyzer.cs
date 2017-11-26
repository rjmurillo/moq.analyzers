using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ShouldNotMockSealedClassesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MOQ1002";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            "Moq: Cannot mock sealed class", "Sealed classes cannot be mocked.", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private const string Category = "Moq";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {

            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            // TODO Think how to make this piece more elegant while fast
            GenericNameSyntax genericName = objectCreation.Type as GenericNameSyntax;
            if (objectCreation.Type is QualifiedNameSyntax)
            {
                var qualifiedName = objectCreation.Type as QualifiedNameSyntax;
                genericName = qualifiedName.Right as GenericNameSyntax;
            }
            if (genericName == null || genericName.Identifier.ToFullString() != "Mock") return;

            if (genericName.TypeArgumentList.Arguments.Count != 1) return;

            var mockedType = genericName.TypeArgumentList.Arguments[0];

            var symbolInfo = context.SemanticModel.GetSymbolInfo(mockedType);

            var symbol = symbolInfo.Symbol as INamedTypeSymbol;

            if (symbol == null) return;

            if (symbol.IsSealed)
            {
                var diagnostic = Diagnostic.Create(Rule, mockedType.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
