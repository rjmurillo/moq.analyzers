using System.Diagnostics;

namespace Moq.Analyzers;

/// <summary>
/// Parameters provided into mock do not match any existing constructors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstructorArgumentsShouldMatchAnalyzer : DiagnosticAnalyzer
{
    internal const string RuleId = "Moq1002";
    private const string Title = "Moq: No matching constructor";
    private const string Message = "Parameters provided into mock do not match any existing constructors";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{RuleId}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(Rule); }
    }

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ObjectCreationExpression);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0051:Method is too long", Justification = "Tracked in #90")]
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        ObjectCreationExpressionSyntax objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        GenericNameSyntax? genericName = GetGenericNameSyntax(objectCreation.Type);
        if (genericName == null) return;

        if (!IsMockGenericType(genericName))
        {
            return;
        }

        // Full check that we are calling new Mock<T>()
        IMethodSymbol? constructorSymbol = GetConstructorSymbol(context, objectCreation);

        // If constructorSymbol is null, we should have caught that earlier (and we cannot proceed)
        if (constructorSymbol == null)
        {
            return;
        }

        // Vararg parameter is the one that takes all arguments for mocked class constructor
        IParameterSymbol? varArgsConstructorParameter = constructorSymbol.Parameters.FirstOrDefault(parameterSymbol => parameterSymbol.IsParams);

        // Vararg parameter are not used, so there are no arguments for mocked class constructor
        if (varArgsConstructorParameter == null)
        {
            return;
        }

        int varArgsConstructorParameterIndex = constructorSymbol.Parameters.IndexOf(varArgsConstructorParameter);

        // Find mocked type
        INamedTypeSymbol? mockedTypeSymbol = GetMockedSymbol(context, genericName);
        if (mockedTypeSymbol == null)
        {
            return;
        }

        // Skip first argument if it is not vararg - typically it is MockingBehavior argument
        IEnumerable<ArgumentSyntax>? constructorArguments = objectCreation.ArgumentList?.Arguments.Skip(varArgsConstructorParameterIndex == 0 ? 0 : 1);

        if (!mockedTypeSymbol.IsAbstract)
        {
            AnalyzeConcrete(context, constructorArguments, objectCreation, genericName);
        }
        else
        {
            // Issue #1: Currently detection does not work well for abstract classes because they cannot be instantiated

            // The mocked symbol is abstract, so we need to check if the constructor arguments match the abstract class constructor
            AnalyzeAbstract(context, constructorArguments, mockedTypeSymbol, objectCreation);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    private static void AnalyzeAbstract(
        SyntaxNodeAnalysisContext context,
        IEnumerable<ArgumentSyntax>? constructorArguments,
        INamedTypeSymbol mockedTypeSymbol,
        ObjectCreationExpressionSyntax objectCreation)
    {
        // Extract types of arguments passed in the constructor call
        if (constructorArguments != null)
        {
            ITypeSymbol[] argumentTypes = constructorArguments
                .Select(arg => context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken).Type)
                .ToArray()!;

            // Check all constructors of the abstract type
            for (int constructorIndex = 0; constructorIndex < mockedTypeSymbol.Constructors.Length; constructorIndex++)
            {
                IMethodSymbol constructor = mockedTypeSymbol.Constructors[constructorIndex];
                if (AreParametersMatching(constructor.Parameters, argumentTypes))
                {
                    return;
                }
            }
        }

        Debug.Assert(objectCreation.ArgumentList != null, "objectCreation.ArgumentList != null");

        Diagnostic diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList?.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeConcrete(
        SyntaxNodeAnalysisContext context,
        IEnumerable<ArgumentSyntax>? constructorArguments,
        ObjectCreationExpressionSyntax objectCreation,
        GenericNameSyntax genericName)
    {
        if (constructorArguments != null
            && IsConstructorMismatch(context, objectCreation, genericName, constructorArguments)
            && objectCreation.ArgumentList != null)
        {
            Diagnostic diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static INamedTypeSymbol? GetMockedSymbol(
        SyntaxNodeAnalysisContext context,
        GenericNameSyntax genericName)
    {
        SeparatedSyntaxList<TypeSyntax> typeArguments = genericName.TypeArgumentList.Arguments;
        if (typeArguments.Count != 1) return null;
        SymbolInfo mockedTypeSymbolInfo = context.SemanticModel.GetSymbolInfo(typeArguments[0], context.CancellationToken);
        if (mockedTypeSymbolInfo.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } mockedTypeSymbol) return null;
        return mockedTypeSymbol;
    }

    private static bool AreParametersMatching(
        ImmutableArray<IParameterSymbol> constructorParameters,
        ITypeSymbol[] argumentTypes)
    {
        // Check if the number of parameters matches
        if (constructorParameters.Length != argumentTypes.Length)
        {
            return false;
        }

        // Check if each parameter type matches in order
        for (int constructorParameterIndex = 0; constructorParameterIndex < constructorParameters.Length; constructorParameterIndex++)
        {
            if (!constructorParameters[constructorParameterIndex].Type.Equals(argumentTypes[constructorParameterIndex], SymbolEqualityComparer.IncludeNullability))
            {
                return false;
            }
        }

        return true;
    }

    private static GenericNameSyntax? GetGenericNameSyntax(TypeSyntax typeSyntax)
    {
        // REVIEW: Switch and ifs are equal in this case, but switch causes AV1535 to trigger
        // The switch expression adds more instructions to do the same, so stick with ifs
        if (typeSyntax is GenericNameSyntax genericNameSyntax)
        {
            return genericNameSyntax;
        }

        if (typeSyntax is QualifiedNameSyntax qualifiedNameSyntax)
        {
            return qualifiedNameSyntax.Right as GenericNameSyntax;
        }

        return null;
    }

    private static bool IsMockGenericType(GenericNameSyntax genericName)
    {
        return string.Equals(genericName.Identifier.Text, "Mock", StringComparison.Ordinal)
               && genericName.TypeArgumentList.Arguments.Count == 1;
    }

    private static IMethodSymbol? GetConstructorSymbol(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation)
    {
        SymbolInfo constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation, context.CancellationToken);
        IMethodSymbol? constructorSymbol = constructorSymbolInfo.Symbol as IMethodSymbol;

        return constructorSymbol?.MethodKind == MethodKind.Constructor &&
               string.Equals(
                   constructorSymbol.ContainingType?.ConstructedFrom.ToDisplayString(),
                   "Moq.Mock<T>",
                   StringComparison.Ordinal)
            ? constructorSymbol
            : null;
    }

    private static bool IsConstructorMismatch(
        SyntaxNodeAnalysisContext context,
        ObjectCreationExpressionSyntax objectCreation,
        GenericNameSyntax genericName,
        IEnumerable<ArgumentSyntax> constructorArguments)
    {
        ObjectCreationExpressionSyntax fakeConstructorCall = SyntaxFactory.ObjectCreationExpression(
            genericName.TypeArgumentList.Arguments.First(),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(constructorArguments)),
            initializer: null);

        SymbolInfo mockedClassConstructorSymbolInfo = context.SemanticModel.GetSpeculativeSymbolInfo(
            objectCreation.SpanStart, fakeConstructorCall, SpeculativeBindingOption.BindAsExpression);

        return mockedClassConstructorSymbolInfo.Symbol == null;
    }
}
