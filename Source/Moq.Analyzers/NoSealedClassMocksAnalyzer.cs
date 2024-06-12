namespace Moq.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoSealedClassMocksAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        Diagnostics.NoSealedClassMocksId,
        Diagnostics.NoSealedClassMocksTitle,
        Diagnostics.NoSealedClassMocksMessage,
        Diagnostics.Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{Diagnostics.NoSealedClassMocksId}.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        ObjectCreationExpressionSyntax? objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        // TODO Think how to make this piece more elegant while fast
        GenericNameSyntax? genericName = objectCreation.Type as GenericNameSyntax;
        if (objectCreation.Type is QualifiedNameSyntax qualifiedName)
        {
            genericName = qualifiedName.Right as GenericNameSyntax;
        }

        if (genericName?.Identifier == null || genericName.TypeArgumentList == null) return;

        // Quick and dirty check
        if (!string.Equals(genericName.Identifier.ToFullString(), "Mock", StringComparison.Ordinal)) return;

        // Full check
        SymbolInfo constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation, context.CancellationToken);
        if (constructorSymbolInfo.Symbol is not IMethodSymbol constructorSymbol || constructorSymbol.ContainingType == null || constructorSymbol.ContainingType.ConstructedFrom == null) return;
        if (constructorSymbol.MethodKind != MethodKind.Constructor) return;
        if (!string.Equals(
                constructorSymbol.ContainingType.ConstructedFrom.ToDisplayString(),
                "Moq.Mock<T>",
                StringComparison.Ordinal)) return;

        // Find mocked type
        SeparatedSyntaxList<TypeSyntax> typeArguments = genericName.TypeArgumentList.Arguments;
        if (typeArguments.Count != 1) return;
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(typeArguments[0], context.CancellationToken);
        if (symbolInfo.Symbol is not INamedTypeSymbol symbol) return;

        // Checked mocked type
        if (symbol.IsSealed && symbol.TypeKind != TypeKind.Delegate)
        {
            Diagnostic? diagnostic = Diagnostic.Create(Rule, typeArguments[0].GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
