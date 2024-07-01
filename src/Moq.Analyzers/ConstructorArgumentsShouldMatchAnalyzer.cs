namespace Moq.Analyzers;

/// <summary>
/// A diagnostic analyzer that ensures the arguments provided to the constructor
/// of a mocked object match an existing constructor of the class being mocked.
/// </summary>
/// <remarks>
/// This analyzer helps catch runtime failures related to constructor mismatches in Moq-based unit tests.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstructorArgumentsShouldMatchAnalyzer : SingleDiagnosticAnalyzer
{
    private const string Description = "Parameters provided into mock do not match any existing constructors.";
    private const string MessageFormat = "Could not find a matching constructor for {0}";
    private const string Title = "Mock<T> construction must call an existing constructor";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.NoMatchingConstructorRuleId,
        Title,
        MessageFormat,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: DiagnosticIds.NoMatchingConstructorRuleId.ToHelpLinkUrl());

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstructorArgumentsShouldMatchAnalyzer"/> class.
    /// </summary>
    public ConstructorArgumentsShouldMatchAnalyzer()
        : base(Rule)
    {
    }

    /// <inheritdoc />
    protected override void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        if (context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.MoqMetadata) == null)
        {
            return;
        }

        // Ensure Moq is referenced in the compilation
        ImmutableArray<INamedTypeSymbol> mockTypes = context.Compilation.GetMoqMock();
        if (mockTypes.IsEmpty)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeNewObject, SyntaxKind.ObjectCreationExpression);
    }

    /// <summary>
    /// Gets the <see cref="GenericNameSyntax"/> from a <see cref="TypeSyntax"/>.
    /// </summary>
    /// <param name="typeSyntax">The type syntax.</param>
    /// <returns>A <see cref="GetGenericNameSyntax"/> when the <paramref name="typeSyntax"/>
    /// is either <see cref="GenericNameSyntax"/> or <see cref="QualifiedNameSyntax"/>; otherwise,
    /// <see langword="null" />.</returns>
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

    /// <summary>
    /// Analyzes when a Mock`1 object is created to verify the provided constructor arguments
    /// match an existing constructor of the mocked class.
    /// </summary>
    /// <param name="context">The context.</param>
    private void AnalyzeNewObject(SyntaxNodeAnalysisContext context)
    {
        ObjectCreationExpressionSyntax objectCreationExpressionSyntax = (ObjectCreationExpressionSyntax)context.Node;

        GenericNameSyntax? genericNameSyntax = GetGenericNameSyntax(objectCreationExpressionSyntax.Type);
        if (genericNameSyntax == null)
        {
            return;
        }

        if (!string.Equals(
                genericNameSyntax.Identifier.ValueText,
                WellKnownTypeNames.MockName,
                StringComparison.Ordinal))
        {
            return;
        }

        SymbolInfo symbolInfo =
            context.SemanticModel.GetSymbolInfo(objectCreationExpressionSyntax, context.CancellationToken);

        if (symbolInfo.Symbol is not IMethodSymbol mockConstructorMethod)
        {
            return;
        }

        if (mockConstructorMethod.ReceiverType is not INamedTypeSymbol { IsGenericType: true } typeSymbol)
        {
            return;
        }

        ITypeSymbol mockedClass = typeSymbol.TypeArguments[0];

        VerifyMockAttempt(context, mockedClass, objectCreationExpressionSyntax.ArgumentList);
    }

    /// <summary>
    /// Checks if the provided arguments match any of the constructors of the mocked class.
    /// </summary>
    /// <param name="constructors">The constructors.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="context">The context.</param>
    /// <returns><c>true</c> if a suitable constructor was found; otherwise <c>false</c>. </returns>
    /// <remarks>Handles <see langword="params" /> and optional parameters.</remarks>
    private bool AnyConstructorsFound(
        ImmutableArray<IMethodSymbol> constructors,
        ImmutableArray<ArgumentSyntax> arguments,
        SyntaxNodeAnalysisContext context)
    {
        for (int constructorIndex = 0; constructorIndex < constructors.Length; constructorIndex++)
        {
            IMethodSymbol constructor = constructors[constructorIndex];
            bool hasParams = constructor.Parameters.Length > 0 && constructor.Parameters[^1].IsParams;
            int fixedParametersCount = hasParams ? constructor.Parameters.Length - 1 : constructor.Parameters.Length;
            int requiredParameters = constructor.Parameters.Count(parameterSymbol => !parameterSymbol.IsOptional);
            bool allParametersMatch = true;

            // Check if the number of arguments is valid considering params
            if ((arguments.Length < fixedParametersCount
                 || (!hasParams && arguments.Length > fixedParametersCount)
                 || (!hasParams && arguments.Length != fixedParametersCount))
                && requiredParameters != arguments.Length)
            {
                continue;
            }

            // There's a chance that there are optional parameters or a ctor that is only optional parameters
            if (arguments.Length <= requiredParameters
                && arguments.Length == 0
                && requiredParameters == 0
                && fixedParametersCount != 0)
            {
                return true;
            }

            // Check fixed parameters
            for (int parameterIndex = 0; parameterIndex < fixedParametersCount; parameterIndex++)
            {
                IParameterSymbol expectedParameter = constructor.Parameters[parameterIndex];

                if (parameterIndex < arguments.Length)
                {
                    ArgumentSyntax passedArgument = arguments[parameterIndex];

                    Conversion conversionType =
                        context.SemanticModel.ClassifyConversion(passedArgument.Expression, expectedParameter.Type);

                    if (!conversionType.Exists)
                    {
                        allParametersMatch = false;
                        break;
                    }
                }
            }

            // Check params parameters if applicable
            if (hasParams && allParametersMatch)
            {
                IParameterSymbol paramsParameter = constructor.Parameters[^1];
                ITypeSymbol paramsElementType = ((IArrayTypeSymbol)paramsParameter.Type).ElementType;

                for (int parameterIndex = fixedParametersCount; parameterIndex < arguments.Length; parameterIndex++)
                {
                    ArgumentSyntax passedArgument = arguments[parameterIndex];
                    Conversion conversionType = context.SemanticModel.ClassifyConversion(passedArgument.Expression, paramsElementType);

                    if (!conversionType.Exists)
                    {
                        allParametersMatch = false;
                        break;
                    }
                }
            }

            if (allParametersMatch)
            {
                return true;
            }
        }

        return false;
    }

    private (bool IsEmpty, Location Location) ConstructorIsEmpty(
        ImmutableArray<IMethodSymbol> constructors,
        ArgumentListSyntax? argumentList,
        SyntaxNodeAnalysisContext context)
    {
        Location location;

        if (argumentList != null)
        {
            location = argumentList.GetLocation();
        }
        else
        {
            location = context.Node.GetLocation();
        }

        return (constructors.IsEmpty, location);
    }

    private bool IsFirstArgumentMockBehavior(ArgumentListSyntax? argumentList)
    {
#pragma warning disable AV1535 // Missing block in case or default clause of switch statement
        switch (argumentList?.Arguments[0].Expression)
        {
            // The first parameter is MockBehavior enum; this is used in the ctor of Mock<T>
            // example: new Mock<T>(MockBehavior.Default)
            case MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.Text: WellKnownTypeNames.MockBehavior }
            }:
                return true;

            // There are other cases when we get into the factory and repository scenarios
            default:
                return false;
        }
#pragma warning restore AV1535 // Missing block in case or default clause of switch statement
    }

    private void VerifyMockAttempt(
                    SyntaxNodeAnalysisContext context,
                    ITypeSymbol mockedClass,
                    ArgumentListSyntax? argumentList)
    {
        if (mockedClass is IErrorTypeSymbol)
        {
            return;
        }

        ImmutableArray<ArgumentSyntax> arguments =
            argumentList?.Arguments.ToImmutableArray() ?? ImmutableArray<ArgumentSyntax>.Empty;

        if (arguments.Length > 0 && IsFirstArgumentMockBehavior(argumentList))
        {
            // They passed a mock behavior as the first argument; ignore as Moq swallows it
            arguments = arguments.RemoveAt(0);
        }

        switch (mockedClass.TypeKind)
        {
            case TypeKind.Interface:
            case TypeKind.Delegate:
                // Interfaces and delegates don't have ctors, so bail out early
                if (arguments.Length == 0)
                {
                    return;
                }

                if (mockedClass.TypeKind == TypeKind.Delegate)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, argumentList?.GetLocation(), argumentList));
                    return;
                }

                break;

            default:
                break;
        }

        // Now we're interested in the ctors for the mocked class
        ImmutableArray<IMethodSymbol> constructors = mockedClass
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(methodSymbol => methodSymbol.MethodKind == MethodKind.Constructor && !methodSymbol.IsStatic)
            .ToImmutableArray();

        // Bail out early if there are no arguments on constructors or no constructors at all
        (bool IsEmpty, Location Location) constructorIsEmpty = ConstructorIsEmpty(constructors, argumentList, context);
        if (constructorIsEmpty.IsEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, constructorIsEmpty.Location, argumentList));
            return;
        }

        // We have constructors, now we need to check if the arguments match any of them
        if (!AnyConstructorsFound(constructors, arguments, context))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, argumentList?.GetLocation(), argumentList));
        }
    }
}
