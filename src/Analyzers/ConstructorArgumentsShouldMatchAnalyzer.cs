namespace Moq.Analyzers;

/// <summary>
/// A diagnostic analyzer that ensures the arguments provided to the constructor
/// of a mocked object match an existing constructor of the class being mocked.
/// </summary>
/// <remarks>
/// This analyzer helps catch runtime failures related to constructor mismatches in Moq-based unit tests.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstructorArgumentsShouldMatchAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ClassMustHaveMatchingConstructor = new(
        DiagnosticIds.NoMatchingConstructorRuleId,
        "Mock<T> construction must call an existing type constructor",
        "Could not find a matching constructor for type '{0}' with arguments {1}",
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Could not find a matching constructor for arguments.",
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.NoMatchingConstructorRuleId}.md");

    private static readonly DiagnosticDescriptor InterfaceMustNotHaveConstructorParameters = new(
        DiagnosticIds.NoConstructorArgumentsForInterfaceMockRuleId,
        "Mock<T> construction must not specify parameters for interfaces",
        "Mocked interface '{0}' cannot have constructor parameters {1}",
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Mocked interface cannot have constructor parameters.",
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.NoConstructorArgumentsForInterfaceMockRuleId}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ClassMustHaveMatchingConstructor, InterfaceMustNotHaveConstructorParameters);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(AnalyzeCompilation);
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
        // REVIEW: Switch and ifs are equal in this case?
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

    private static bool IsExpressionMockBehavior(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols, ExpressionSyntax? expression)
    {
        if (expression is null)
        {
            return false;
        }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);

        if (symbolInfo.Symbol is null)
        {
            return false;
        }

        ISymbol targetSymbol = GetUnderlyingTypeSymbol(symbolInfo.Symbol);
        return targetSymbol.IsInstanceOf(knownSymbols.MockBehavior);
    }

    /// <summary>
    /// Extracts the type symbol from a symbol that may be a parameter, local, or field.
    /// </summary>
    private static ISymbol GetUnderlyingTypeSymbol(ISymbol symbol)
    {
        return symbol switch
        {
            IParameterSymbol parameterSymbol => parameterSymbol.Type,
            ILocalSymbol localSymbol => localSymbol.Type,
            IFieldSymbol fieldSymbol => fieldSymbol.Type,
            _ => symbol,
        };
    }

    private static bool IsArgumentMockBehavior(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols, ArgumentListSyntax? argumentList, uint argumentOrdinal)
    {
        ExpressionSyntax? expression = argumentList?.Arguments.Count > argumentOrdinal ? argumentList.Arguments[(int)argumentOrdinal].Expression : null;

        return IsExpressionMockBehavior(context, knownSymbols, expression);
    }

    private static string FormatArguments(ArgumentSyntax[] arguments)
    {
        if (arguments.Length == 0)
        {
            return "()";
        }

        return $"({string.Join(", ", arguments.Select(arg => arg.Expression.ToString()))})";
    }

    private static void VerifyDelegateMockAttempt(
    SyntaxNodeAnalysisContext context,
    ITypeSymbol mockedDelegate,
    ArgumentListSyntax? argumentList,
    ArgumentSyntax[] arguments)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        string argumentsString = FormatArguments(arguments);
        Diagnostic? diagnostic = argumentList?.CreateDiagnostic(ClassMustHaveMatchingConstructor, mockedDelegate.ToDisplayString(), argumentsString);
        if (diagnostic != null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void VerifyInterfaceMockAttempt(
        SyntaxNodeAnalysisContext context,
        ITypeSymbol mockedInterface,
        ArgumentListSyntax? argumentList,
        ArgumentSyntax[] arguments)
    {
        // Interfaces and delegates don't have ctors, so bail out early
        if (arguments.Length == 0)
        {
            return;
        }

        string argumentsString = FormatArguments(arguments);
        Diagnostic? diagnostic = argumentList?.CreateDiagnostic(InterfaceMustNotHaveConstructorParameters, mockedInterface.ToDisplayString(), argumentsString);
        if (diagnostic != null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // We're interested in the few ways to create mocks:
        //  - new Mock<T>()
        //  - Mock.Of<T>()
        //  - MockRepository.Create<T>()
        //
        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // These are for classes
        context.RegisterSyntaxNodeAction(context => AnalyzeNewObject(context, knownSymbols), SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(context => AnalyzeInstanceCall(context, knownSymbols), SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInstanceCall(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        InvocationExpressionSyntax invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

        AnalyzeInvocation(context, knownSymbols, invocationExpressionSyntax);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        MoqKnownSymbols knownSymbols,
        InvocationExpressionSyntax invocationExpressionSyntax)
    {
        SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax, context.CancellationToken);

        if (symbol.Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (method.IsInstanceOf(knownSymbols.MockOf))
        {
            // Mock.Of<T> cannot specify constructor parameters.
            // The mocked type is the return type directly (not wrapped in Mock<T>).
            VerifyMockAttempt(context, knownSymbols, method.ReturnType, argumentList: null, hasMockBehavior: true);
            return;
        }

        if (!method.IsInstanceOf(knownSymbols.MockRepositoryCreate))
        {
            return;
        }

        // MockRepository.Create<T> returns Mock<T>; extract T.
        if (method.ReturnType is not INamedTypeSymbol { IsGenericType: true } typeSymbol)
        {
            return;
        }

        VerifyMockAttempt(context, knownSymbols, typeSymbol.TypeArguments[0], invocationExpressionSyntax.ArgumentList, hasMockBehavior: true);
    }

    /// <summary>
    /// Analyzes when a Mock`1 object is created to verify the provided constructor arguments
    /// match an existing constructor of the mocked class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="knownSymbols">The <see cref="MoqKnownSymbols"/> used to lookup symbols against Moq types.</param>
    private static void AnalyzeNewObject(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        ObjectCreationExpressionSyntax newExpression = (ObjectCreationExpressionSyntax)context.Node;

        GenericNameSyntax? genericNameSyntax = GetGenericNameSyntax(newExpression.Type);
        if (genericNameSyntax == null)
        {
            return;
        }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(newExpression, context.CancellationToken);

        if (!symbolInfo
            .Symbol?
            .IsInstanceOf(knownSymbols.Mock1?.Constructors ?? ImmutableArray<IMethodSymbol>.Empty)
            ?? false)
        {
            return;
        }

        if (symbolInfo.Symbol?.ContainingType is not INamedTypeSymbol { IsGenericType: true } typeSymbol)
        {
            return;
        }

        ITypeSymbol mockedClass = typeSymbol.TypeArguments[0];

        VerifyMockAttempt(context, knownSymbols, mockedClass, newExpression.ArgumentList, true);
    }

    /// <summary>
    /// Checks if the provided arguments match any of the constructors of the mocked class.
    /// </summary>
    /// <param name="constructors">The constructors.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="context">The context.</param>
    /// <returns>
    /// <see langword="true" /> if a suitable constructor was found; otherwise <see langword="false" />.
    /// If the construction method is a parenthesized lambda expression, <see langword="null" /> is returned.
    /// </returns>
    /// <remarks>Handles <see langword="params" /> and optional parameters.</remarks>
    private static bool? AnyConstructorsFound(
        IMethodSymbol[] constructors,
        ArgumentSyntax[] arguments,
        SyntaxNodeAnalysisContext context)
    {
        for (int constructorIndex = 0; constructorIndex < constructors.Length; constructorIndex++)
        {
            if (IsConstructorMatch(constructors[constructorIndex], arguments, context))
            {
                return true;
            }
        }

        // Special case: parenthesized lambda creates the instance directly.
        // The compiler validates the lambda, so no additional checks are needed.
        // See https://github.com/devlooped/moq/blob/18dc7410ad4f993ce0edd809c5dfcaa3199f13ff/src/Moq/Mock%601.cs#L200
        if (arguments.Length == 1 && arguments[0].Expression.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
        {
            return null;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the given constructor accepts the provided arguments.
    /// </summary>
    /// <param name="constructor">The constructor to check.</param>
    /// <param name="arguments">The arguments provided at the call site.</param>
    /// <param name="context">The syntax node analysis context.</param>
    /// <returns><see langword="true"/> if the constructor matches the arguments; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Handles <see langword="params"/> and optional parameters.</remarks>
    private static bool IsConstructorMatch(
        IMethodSymbol constructor,
        ArgumentSyntax[] arguments,
        SyntaxNodeAnalysisContext context)
    {
        bool hasParams = constructor.Parameters.Length > 0 && constructor.Parameters[^1].IsParams;
        int fixedParametersCount = hasParams ? constructor.Parameters.Length - 1 : constructor.Parameters.Length;
#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
        int requiredParameters = constructor.Parameters.Count(parameterSymbol => !parameterSymbol.IsOptional);
#pragma warning restore ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing

        if (!IsArgumentCountValid(arguments.Length, fixedParametersCount, requiredParameters, hasParams))
        {
            return false;
        }

        // All parameters are optional and no arguments were provided.
        if (arguments.Length == 0 && requiredParameters == 0 && fixedParametersCount != 0)
        {
            return true;
        }

        if (!AllFixedParametersMatch(constructor, arguments, fixedParametersCount, context))
        {
            return false;
        }

        if (hasParams && !AllParamsArgumentsMatch(constructor, arguments, fixedParametersCount, context))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether the number of arguments is valid for the constructor signature.
    /// </summary>
    private static bool IsArgumentCountValid(int argumentCount, int fixedParametersCount, int requiredParameters, bool hasParams)
    {
        // When the argument count matches the required parameter count, it is valid.
        if (argumentCount == requiredParameters)
        {
            return true;
        }

        // For params constructors, the argument count must be at least the fixed parameter count.
        if (hasParams)
        {
            return argumentCount >= fixedParametersCount;
        }

        // For non-params constructors, the argument count must equal the fixed parameter count.
        return argumentCount == fixedParametersCount;
    }

    /// <summary>
    /// Verifies that all fixed (non-params) arguments are convertible to the expected parameter types.
    /// </summary>
    private static bool AllFixedParametersMatch(
        IMethodSymbol constructor,
        ArgumentSyntax[] arguments,
        int fixedParametersCount,
        SyntaxNodeAnalysisContext context)
    {
        for (int parameterIndex = 0; parameterIndex < fixedParametersCount; parameterIndex++)
        {
            if (parameterIndex >= arguments.Length)
            {
                continue;
            }

            IParameterSymbol expectedParameter = constructor.Parameters[parameterIndex];
            Conversion conversionType = context.SemanticModel.ClassifyConversion(arguments[parameterIndex].Expression, expectedParameter.Type);

            if (!conversionType.Exists)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Verifies that all arguments in the params position are convertible to the params element type.
    /// </summary>
    private static bool AllParamsArgumentsMatch(
        IMethodSymbol constructor,
        ArgumentSyntax[] arguments,
        int fixedParametersCount,
        SyntaxNodeAnalysisContext context)
    {
        IParameterSymbol paramsParameter = constructor.Parameters[^1];
        ITypeSymbol paramsElementType = ((IArrayTypeSymbol)paramsParameter.Type).ElementType;

        for (int parameterIndex = fixedParametersCount; parameterIndex < arguments.Length; parameterIndex++)
        {
            Conversion conversionType = context.SemanticModel.ClassifyConversion(arguments[parameterIndex].Expression, paramsElementType);

            if (!conversionType.Exists)
            {
                return false;
            }
        }

        return true;
    }

    private static (bool IsEmpty, Location Location) ConstructorIsEmpty(
        IMethodSymbol[] constructors,
        ArgumentListSyntax? argumentList,
        SyntaxNodeAnalysisContext context)
    {
        Location location = argumentList?.GetLocation() ?? context.Node.GetLocation();
        return (constructors.Length == 0, location);
    }

    private static void VerifyMockAttempt(
                    SyntaxNodeAnalysisContext context,
                    MoqKnownSymbols knownSymbols,
                    ITypeSymbol mockedClass,
                    ArgumentListSyntax? argumentList,
                    bool hasMockBehavior)
    {
        if (mockedClass is IErrorTypeSymbol)
        {
            return;
        }

#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
        ArgumentSyntax[] arguments = argumentList?.Arguments.ToArray() ?? [];
#pragma warning restore ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing

        if (hasMockBehavior && arguments.Length > 0)
        {
            if (arguments.Length >= 1 && IsArgumentMockBehavior(context, knownSymbols, argumentList, 0))
            {
                // They passed a mock behavior as the first argument; ignore as Moq swallows it
                arguments = arguments.RemoveAt(0);
            }
            else if (arguments.Length >= 2 && IsArgumentMockBehavior(context, knownSymbols, argumentList, 1))
            {
                // They passed a mock behavior as the second argument; ignore as Moq swallows it
                arguments = arguments.RemoveAt(1);
            }
        }

        switch (mockedClass.TypeKind)
        {
            case TypeKind.Interface:
                VerifyInterfaceMockAttempt(context, mockedClass, argumentList, arguments);
                break;

            case TypeKind.Delegate:
                // Interfaces and delegates don't have ctors, so bail out early
                VerifyDelegateMockAttempt(context, mockedClass, argumentList, arguments);
                break;

            case TypeKind.Class:
                // Now we're interested in the ctors for the mocked class
                VerifyClassMockAttempt(context, mockedClass, argumentList, arguments);

                break;
        }
    }

    private static void VerifyClassMockAttempt(
        SyntaxNodeAnalysisContext context,
        ITypeSymbol mockedClass,
        ArgumentListSyntax? argumentList,
        ArgumentSyntax[] arguments)
    {
        IMethodSymbol[] constructors = mockedClass
            .GetMembers(WellKnownMemberNames.InstanceConstructorName)
            .OfType<IMethodSymbol>()
            .Where(methodSymbol => methodSymbol.IsConstructor())
            .ToArray();

        string argumentsString = FormatArguments(arguments);

        // Bail out early if there are no arguments on constructors or no constructors at all
        (bool IsEmpty, Location Location) constructorIsEmpty = ConstructorIsEmpty(constructors, argumentList, context);
        if (constructorIsEmpty.IsEmpty)
        {
            Diagnostic diagnostic = constructorIsEmpty.Location.CreateDiagnostic(ClassMustHaveMatchingConstructor, mockedClass.ToDisplayString(), argumentsString);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // We have constructors, now we need to check if the arguments match any of them
        // If the value is null it means we want to ignore and not create a diagnostic
        bool? matchingCtorFound = AnyConstructorsFound(constructors, arguments, context);
        if (matchingCtorFound.HasValue && !matchingCtorFound.Value)
        {
            Diagnostic diagnostic = constructorIsEmpty.Location.CreateDiagnostic(ClassMustHaveMatchingConstructor, mockedClass.ToDisplayString(), argumentsString);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
