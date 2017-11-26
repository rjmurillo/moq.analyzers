using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Moq.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MatchingConstructorParametersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MOQ1003";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            "Moq: No constructors with such parameters", "Parameters provided into mock do not match existing constructors.", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
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
            if (genericName?.Identifier == null || genericName.TypeArgumentList == null) return;

            // Quick and dirty check
            if (genericName.Identifier.ToFullString() != "Mock") return;

            // Full check
            var constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation);
            var constructorSymbol = constructorSymbolInfo.Symbol as IMethodSymbol;
            if (constructorSymbol == null || constructorSymbol.ContainingType == null || constructorSymbol.ContainingType.ConstructedFrom == null) return;
            if (constructorSymbol.MethodKind != MethodKind.Constructor) return;
            if (constructorSymbol.ContainingType.ConstructedFrom.ToDisplayString() != "Moq.Mock<T>") return;
            if (constructorSymbol.Parameters == null || constructorSymbol.Parameters.Length == 0) return;

            var varArgsConstructorParameter = constructorSymbol.Parameters.FirstOrDefault(x => x.IsParams);
            // Varargs are not used, so no true parameters for the mocked class
            if (varArgsConstructorParameter == null) return;
            var varArgIdx = constructorSymbol.Parameters.IndexOf(varArgsConstructorParameter);

            // Find mocked type
            var typeArguments = genericName.TypeArgumentList.Arguments;
            if (typeArguments == null || typeArguments.Count != 1) return;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(typeArguments[0]);
            var symbol = symbolInfo.Symbol as INamedTypeSymbol;
            if (symbol == null) return;
            bool matchingConstructorFound = false;
            // Skip first argument if it is not vararg
            var objectCreationArguments = objectCreation.ArgumentList.Arguments.Skip(varArgIdx == 0 ? 0 : 1).ToArray();
            foreach (var mockConstructorSymbol in symbol.Constructors)
            {
                if (mockConstructorSymbol.Parameters.Length != objectCreationArguments.Length) continue;
                bool allArgumentsMatch = true;
                for (int i = 0; i < objectCreationArguments.Length; i++)
                {
                    var objectCreationArgument = objectCreationArguments[i];
                    var argumentSymbolInfo = context.SemanticModel.GetTypeInfo(objectCreationArgument.Expression);
                    if (argumentSymbolInfo.Type == null) break;
                    var mockConstructorParameter = mockConstructorSymbol.Parameters[i];
                    var conversion = context.SemanticModel.Compilation.ClassifyConversion(argumentSymbolInfo.Type, mockConstructorParameter.Type);
                    if (!conversion.Exists)
                    {
                        allArgumentsMatch = false;
                        break;
                    }
                }
                if (allArgumentsMatch)
                {
                    matchingConstructorFound = true;
                    break;
                }
            }

            // Checked mocked type
            if (!matchingConstructorFound)
            {
                var diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
