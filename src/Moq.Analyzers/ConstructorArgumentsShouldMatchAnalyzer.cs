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
        // Pre-requisite checks
        ObjectCreationExpressionSyntax objectCreation = (ObjectCreationExpressionSyntax)context.Node;
        GenericNameSyntax? genericName = GetGenericNameSyntax(objectCreation.Type);
        if (genericName == null)
        {
            return;
        }

        if (!IsMockGenericType(genericName)) return;

        // Full check that we are calling new Mock<T>()
        IMethodSymbol? mockCtorSymbol = GetConstructorSymbol(context, objectCreation);

        if (mockCtorSymbol is null)
        {
            return;
        }

        // Find mocked type
        INamedTypeSymbol? mockedTypeSymbol = GetMockedSymbol(context, genericName);
        if (mockedTypeSymbol == null)
        {
            return;
        }

        // All the basic checks are done, now we need to check if the constructor arguments match
        // the mocked class constructor

        // Vararg parameter is the one that takes all arguments for mocked class constructor
        IParameterSymbol? varArgsConstructorParameter = mockCtorSymbol.Parameters.FirstOrDefault(parameterSymbol => parameterSymbol.IsParams);

        // Vararg parameter are not used, so there are no arguments for mocked type constructor
        if (varArgsConstructorParameter == null)
        {
            // Check if the mocked type has a default constructor or a constructor with all optional parameters
            if (mockedTypeSymbol.Constructors.Any(methodSymbol => methodSymbol.Parameters.Length == 0
                || methodSymbol.Parameters.All(parameterSymbol => parameterSymbol.HasExplicitDefaultValue)))
            {
                return;
            }

            // There is no default constructor on the mocked type
            Diagnostic diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList?.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            int varArgsConstructorParameterIndex = mockCtorSymbol.Parameters.IndexOf(varArgsConstructorParameter);

            // Skip first argument if it is not vararg - typically it is MockingBehavior argument
            ArgumentSyntax[]? constructorArguments = objectCreation.ArgumentList?.Arguments
                .Skip(varArgsConstructorParameterIndex == 0 ? 0 : 1).ToArray();

            if (mockedTypeSymbol.IsAbstract)
            {
                // Issue #1: Currently detection does not work well for abstract classes because they cannot be instantiated

                // The mocked symbol is abstract, so we need to check if the constructor arguments match the abstract class constructor

                // Extract types of arguments passed in the constructor call
                if (constructorArguments != null)
                {
                    ITypeSymbol[] argumentTypes = constructorArguments
                        .Select(arg =>
                            context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken).Type)
                        .ToArray()!;

                    // Check all constructors of the abstract type
                    for (int constructorIndex = 0;
                         constructorIndex < mockedTypeSymbol.Constructors.Length;
                         constructorIndex++)
                    {
                        IMethodSymbol constructor = mockedTypeSymbol.Constructors[constructorIndex];
                        if (AreParametersMatching(constructor.Parameters, argumentTypes))
                        {
                            return; // Found a matching constructor
                        }
                    }
                }

                Debug.Assert(objectCreation.ArgumentList != null, "objectCreation.ArgumentList != null");

                Diagnostic diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList?.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                if (constructorArguments != null
                    && IsConstructorMismatch(context, objectCreation, genericName, constructorArguments)
                    && objectCreation.ArgumentList != null)
                {
                    Diagnostic diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
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
        ITypeSymbol[] argumentTypes2)
    {
        // Check if the number of parameters matches
        if (constructorParameters.Length != argumentTypes2.Length)
        {
            return false;
        }

        // Check if each parameter type matches in order
        for (int constructorParameterIndex = 0; constructorParameterIndex < constructorParameters.Length; constructorParameterIndex++)
        {
            if (!constructorParameters[constructorParameterIndex].Type.Equals(argumentTypes2[constructorParameterIndex], SymbolEqualityComparer.IncludeNullability))
            {
                return false;
            }
        }

        return true;
    }

    private static GenericNameSyntax? GetGenericNameSyntax(TypeSyntax typeSyntax)
    {
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

    private static bool IsConstructorMismatch(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation, GenericNameSyntax genericName, ArgumentSyntax[] constructorArguments)
    {
        ObjectCreationExpressionSyntax fakeConstructorCall = SyntaxFactory.ObjectCreationExpression(
            genericName.TypeArgumentList.Arguments.First(),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(constructorArguments)),
            null);

        SymbolInfo mockedClassConstructorSymbolInfo = context.SemanticModel.GetSpeculativeSymbolInfo(
            objectCreation.SpanStart, fakeConstructorCall, SpeculativeBindingOption.BindAsExpression);

        return mockedClassConstructorSymbolInfo.Symbol == null;
    }
}
