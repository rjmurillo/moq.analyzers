using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoConstructorArgumentsForInterfaceMockAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        Diagnostics.NoConstructorArgumentsForInterfaceMockId,
        Diagnostics.NoConstructorArgumentsForInterfaceMockTitle,
        Diagnostics.NoConstructorArgumentsForInterfaceMockMessage,
        Diagnostics.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

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
        var constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation, context.CancellationToken);
        var constructorSymbol = constructorSymbolInfo.Symbol as IMethodSymbol;
        if (constructorSymbol == null || constructorSymbol.ContainingType == null || constructorSymbol.ContainingType.ConstructedFrom == null) return;
        if (constructorSymbol.MethodKind != MethodKind.Constructor) return;
        if (constructorSymbol.ContainingType.ConstructedFrom.ToDisplayString() != "Moq.Mock<T>") return;
        if (constructorSymbol.Parameters == null || constructorSymbol.Parameters.Length == 0) return;
        if (!constructorSymbol.Parameters.Any(x => x.IsParams)) return;

        // Find mocked type
        var typeArguments = genericName.TypeArgumentList.Arguments;
        if (typeArguments == null || typeArguments.Count != 1) return;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(typeArguments[0], context.CancellationToken);
        var symbol = symbolInfo.Symbol as INamedTypeSymbol;
        if (symbol == null) return;

        // Checked mocked type
        if (symbol.TypeKind == TypeKind.Interface)
        {
            var diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
