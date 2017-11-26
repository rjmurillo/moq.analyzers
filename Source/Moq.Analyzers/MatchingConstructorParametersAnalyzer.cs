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
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Diagnostics.NoSealedClassMocksId,
            Diagnostics.NoSealedClassMocksTitle,
            Diagnostics.NoSealedClassMocksMessage,
            Diagnostics.Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

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

            // Quick and dirty check that we are calling new Mock<T>()
            if (genericName.Identifier.ToFullString() != "Mock") return;

            // Full check that we are calling new Mock<T>()
            var constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation);
            var constructorSymbol = constructorSymbolInfo.Symbol as IMethodSymbol;
            if (constructorSymbol == null || constructorSymbol.ContainingType == null || constructorSymbol.ContainingType.ConstructedFrom == null) return;
            if (constructorSymbol.MethodKind != MethodKind.Constructor) return;
            if (constructorSymbol.ContainingType.ConstructedFrom.ToDisplayString() != "Moq.Mock<T>") return;
            if (constructorSymbol.Parameters == null || constructorSymbol.Parameters.Length == 0) return;

            // Vararg parameter is the one that takes all arguments for mocked class constructor
            var varArgsConstructorParameter = constructorSymbol.Parameters.FirstOrDefault(x => x.IsParams);
            // Vararg parameter are not used, so there are no arguments for mocked class constructor
            if (varArgsConstructorParameter == null) return;
            var varArgsConstructorParameterIdx = constructorSymbol.Parameters.IndexOf(varArgsConstructorParameter);

            // Find mocked type
            var typeArguments = genericName.TypeArgumentList.Arguments;
            if (typeArguments == null || typeArguments.Count != 1) return;
            var mockedTypeSymbolInfo = context.SemanticModel.GetSymbolInfo(typeArguments[0]);
            var mockedTypeSymbol = mockedTypeSymbolInfo.Symbol as INamedTypeSymbol;
            if (mockedTypeSymbol == null || mockedTypeSymbol.TypeKind != TypeKind.Class) return;
            
            // TODO: Currently detection does not work well for abstract classes because they cannot be instantiated
            if (mockedTypeSymbol.IsAbstract) return;

            // Skip first argument if it is not vararg - typically it is MockingBehavior argument
            var constructorArguments = objectCreation.ArgumentList.Arguments.Skip(varArgsConstructorParameterIdx == 0 ? 0 : 1).ToArray();

            // Build fake constructor call to understand how it is resolved
            var fakeConstructorCall = SyntaxFactory.ObjectCreationExpression(typeArguments[0], SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(constructorArguments)), null);
            var mockedClassConstructorSymbolInfo = context.SemanticModel.GetSpeculativeSymbolInfo(objectCreation.GetLocation().SourceSpan.Start, fakeConstructorCall, SpeculativeBindingOption.BindAsExpression);
            if (mockedClassConstructorSymbolInfo.Symbol == null)
            {
                var diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
