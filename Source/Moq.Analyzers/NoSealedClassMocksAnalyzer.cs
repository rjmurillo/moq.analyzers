using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoSealedClassMocksAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        Diagnostics.NoSealedClassMocksId,
        Diagnostics.NoSealedClassMocksTitle,
        Diagnostics.NoSealedClassMocksMessage,
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
        ObjectCreationExpressionSyntax? objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        // TODO Think how to make this piece more elegant while fast
        GenericNameSyntax genericName = objectCreation.Type as GenericNameSyntax;
        if (objectCreation.Type is QualifiedNameSyntax)
        {
            QualifiedNameSyntax? qualifiedName = objectCreation.Type as QualifiedNameSyntax;
            genericName = qualifiedName.Right as GenericNameSyntax;
        }

        if (genericName?.Identifier == null || genericName.TypeArgumentList == null) return;

        // Quick and dirty check
        if (genericName.Identifier.ToFullString() != "Mock") return;

        // Full check
        SymbolInfo constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation);
        IMethodSymbol? constructorSymbol = constructorSymbolInfo.Symbol as IMethodSymbol;
        if (constructorSymbol == null || constructorSymbol.ContainingType == null || constructorSymbol.ContainingType.ConstructedFrom == null) return;
        if (constructorSymbol.MethodKind != MethodKind.Constructor) return;
        if (constructorSymbol.ContainingType.ConstructedFrom.ToDisplayString() != "Moq.Mock<T>") return;

        // Find mocked type
        SeparatedSyntaxList<TypeSyntax> typeArguments = genericName.TypeArgumentList.Arguments;
        if (typeArguments == null || typeArguments.Count != 1) return;
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(typeArguments[0]);
        INamedTypeSymbol? symbol = symbolInfo.Symbol as INamedTypeSymbol;
        if (symbol == null) return;

        // Checked mocked type
        if (symbol.IsSealed && symbol.TypeKind != TypeKind.Delegate)
        {
            Diagnostic? diagnostic = Diagnostic.Create(Rule, typeArguments[0].GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
