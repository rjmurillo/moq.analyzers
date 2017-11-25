using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Moq.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ShouldNotAllowParametersForMockedInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Moq1003";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            "Moq: Parameters for mocked interface", "Do not specify parameters for mocked interface.", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
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

            if (symbol.TypeKind == TypeKind.Interface)
            {
                var arguments = objectCreation.ArgumentList.Arguments;
                if (arguments.Count > 0)
                {
                    var constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation);
                    var constructorSymbol = constructorSymbolInfo.Symbol as IMethodSymbol;
                    if (constructorSymbol == null) return;
                    if (constructorSymbol.MethodKind != MethodKind.Constructor) return;
                    if (constructorSymbol.Parameters.Length > 0)
                    {
                        var firstParameter = constructorSymbol.Parameters[0];
                        if (firstParameter.IsParams || (constructorSymbol.Parameters.Length > 1 && constructorSymbol.Parameters[1].IsParams))
                        {
                            var diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
