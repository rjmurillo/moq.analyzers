using System.Diagnostics.CodeAnalysis;

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
        "Could not find a matching constructor for arguments {0}",
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters provided into mock do not match any existing constructors.",
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.NoMatchingConstructorRuleId}.md");

    private static readonly DiagnosticDescriptor InterfaceMustNotHaveConstructorParameters = new(
        DiagnosticIds.NoConstructorArgumentsForInterfaceMockRuleId,
        "Mock<T> construction must not specify parameters for interfaces",
        "Mocked interface cannot have constructor parameters {0}",
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Mock of interface cannot contain constructor parameters.",
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

    private static bool IsExpressionMockBehavior(SyntaxNodeAnalysisContext context, ExpressionSyntax? expression)
    {
        if (expression == null)
        {
            return false;
        }

        if (expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            if (memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax
                && string.Equals(identifierNameSyntax.Identifier.ValueText, WellKnownTypeNames.MockBehavior, StringComparison.Ordinal))
            {
                return true;
            }
        }
        else if (expression is IdentifierNameSyntax identifierNameSyntax)
        {
            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(identifierNameSyntax, context.CancellationToken);

            if (symbolInfo.Symbol == null)
            {
                return false;
            }

            ITypeSymbol? typeSymbol = null;
            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                typeSymbol = parameterSymbol.Type;
            }
            else if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                typeSymbol = localSymbol.Type;
            }
            else if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
            {
                typeSymbol = fieldSymbol.Type;
            }

            if (typeSymbol != null
                && string.Equals(typeSymbol.Name, WellKnownTypeNames.MockBehavior, StringComparison.Ordinal))
            {
                return true;
            }
        }

        // Crude fallback to check if the expression is a Moq.MockBehavior enum
        if (expression.ToString().StartsWith(WellKnownTypeNames.MoqBehavior, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static bool IsFirstArgumentMockBehavior(SyntaxNodeAnalysisContext context, ArgumentListSyntax? argumentList)
    {
        ExpressionSyntax? expression = argumentList?.Arguments[0].Expression;

        return IsExpressionMockBehavior(context, expression);
    }

    private static void VerifyDelegateMockAttempt(
    SyntaxNodeAnalysisContext context,
    ArgumentListSyntax? argumentList,
    ArgumentSyntax[] arguments)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        Diagnostic? diagnostic = argumentList?.GetLocation().CreateDiagnostic(ClassMustHaveMatchingConstructor, argumentList);
        if (diagnostic != null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void VerifyInterfaceMockAttempt(
        SyntaxNodeAnalysisContext context,
        ArgumentListSyntax? argumentList,
        ArgumentSyntax[] arguments)
    {
        // Interfaces and delegates don't have ctors, so bail out early
        if (arguments.Length == 0)
        {
            return;
        }

        Diagnostic? diagnostic = argumentList?.GetLocation().CreateDiagnostic(InterfaceMustNotHaveConstructorParameters, argumentList);
        if (diagnostic != null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.Compilation.Options.IsAnalyzerSuppressed(InterfaceMustNotHaveConstructorParameters)
            && context.Compilation.Options.IsAnalyzerSuppressed(ClassMustHaveMatchingConstructor))
        {
            return;
        }

        // We're interested in the few ways to create mocks:
        //  - new Mock<T>()
        //  - Mock.Of<T>()
        //  - MockRepository.Create<T>()
        //
        // Ensure Moq is referenced in the compilation
        ImmutableArray<INamedTypeSymbol> mockTypes = context.Compilation.GetMoqMock();
        if (mockTypes.IsEmpty)
        {
            return;
        }

        // These are for classes
        context.RegisterSyntaxNodeAction(AnalyzeNewObject, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInstanceCall, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInstanceCall(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

        if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            return;
        }

        if (memberAccessExpressionSyntax.Name is not GenericNameSyntax genericNameSyntax)
        {
            return;
        }

        switch (genericNameSyntax.Identifier.Value)
        {
            case WellKnownTypeNames.Create:
                AnalyzeInvocation(context, invocationExpressionSyntax, WellKnownTypeNames.MockFactory, true, true);
                break;

            case WellKnownTypeNames.Of:
                AnalyzeInvocation(context, invocationExpressionSyntax, WellKnownTypeNames.MockName, false, true);
                break;

            default:
                return;
        }
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocationExpressionSyntax,
        string expectedClassName,
        bool hasReturnedMock,
        bool hasMockBehavior)
    {
        SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax, context.CancellationToken);

        if (symbol.Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (!string.Equals(method.ContainingType.Name, expectedClassName, StringComparison.Ordinal))
        {
            return;
        }

        ITypeSymbol returnType = method.ReturnType;
        if (hasReturnedMock)
        {
            if (returnType is not INamedTypeSymbol { IsGenericType: true } typeSymbol)
            {
                return;
            }

            returnType = typeSymbol.TypeArguments[0];
        }

        // We are calling MockRepository.Create<T> or Mock.Of<T>, determine which
        ArgumentListSyntax? argumentList = null;
        if (WellKnownTypeNames.Of.Equals(method.Name, StringComparison.Ordinal))
        {
            // Mock.Of<T> can specify condition for construction and MockBehavior, but
            // cannot specify constructor parameters
            //
            // The only parameters that can be passed are not relevant for verification
            // to just strip them
        }
        else
        {
            argumentList = invocationExpressionSyntax.ArgumentList;
        }

        VerifyMockAttempt(context, returnType, argumentList, hasMockBehavior);
    }

    /// <summary>
    /// Analyzes when a Mock`1 object is created to verify the provided constructor arguments
    /// match an existing constructor of the mocked class.
    /// </summary>
    /// <param name="context">The context.</param>
    private static void AnalyzeNewObject(SyntaxNodeAnalysisContext context)
    {
        ObjectCreationExpressionSyntax newExpression = (ObjectCreationExpressionSyntax)context.Node;

        GenericNameSyntax? genericNameSyntax = GetGenericNameSyntax(newExpression.Type);
        if (genericNameSyntax == null)
        {
            return;
        }

        // Quick check
        if (!string.Equals(
                genericNameSyntax.Identifier.ValueText,
                WellKnownTypeNames.MockName,
                StringComparison.Ordinal))
        {
            return;
        }

        // Full check
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(newExpression, context.CancellationToken);

        if (symbolInfo.Symbol is not IMethodSymbol mockConstructorMethod
            || mockConstructorMethod.MethodKind != MethodKind.Constructor
            || !string.Equals(mockConstructorMethod.ContainingType.ConstructedFrom.ContainingSymbol.Name, WellKnownTypeNames.Moq, StringComparison.Ordinal))
        {
            return;
        }

        if (mockConstructorMethod.ReceiverType is not INamedTypeSymbol { IsGenericType: true } typeSymbol)
        {
            return;
        }

        ITypeSymbol mockedClass = typeSymbol.TypeArguments[0];

        VerifyMockAttempt(context, mockedClass, newExpression.ArgumentList, true);
    }

    /// <summary>
    /// Checks if the provided arguments match any of the constructors of the mocked class.
    /// </summary>
    /// <param name="constructors">The constructors.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="context">The context.</param>
    /// <returns><c>true</c> if a suitable constructor was found; otherwise <c>false</c>. </returns>
    /// <remarks>Handles <see langword="params" /> and optional parameters.</remarks>
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "This should be refactored; suppressing for now to enable TreatWarningsAsErrors in CI.")]
    private static bool AnyConstructorsFound(
        IMethodSymbol[] constructors,
        ArgumentSyntax[] arguments,
        SyntaxNodeAnalysisContext context)
    {
        for (int constructorIndex = 0; constructorIndex < constructors.Length; constructorIndex++)
        {
            IMethodSymbol constructor = constructors[constructorIndex];
            bool hasParams = constructor.Parameters.Length > 0 && constructor.Parameters[^1].IsParams;
            int fixedParametersCount = hasParams ? constructor.Parameters.Length - 1 : constructor.Parameters.Length;
#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
            int requiredParameters = constructor.Parameters.Count(parameterSymbol => !parameterSymbol.IsOptional);
#pragma warning restore ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
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

    private static (bool IsEmpty, Location Location) ConstructorIsEmpty(
        IMethodSymbol[] constructors,
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

        return (constructors.Length == 0, location);
    }

    private static void VerifyMockAttempt(
                    SyntaxNodeAnalysisContext context,
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

        if (hasMockBehavior && arguments.Length > 0 && IsFirstArgumentMockBehavior(context, argumentList))
        {
            // They passed a mock behavior as the first argument; ignore as Moq swallows it
            arguments = arguments.RemoveAt(0);
        }

        switch (mockedClass.TypeKind)
        {
            case TypeKind.Interface:
                VerifyInterfaceMockAttempt(context, argumentList, arguments);
                break;

            case TypeKind.Delegate:
                // Interfaces and delegates don't have ctors, so bail out early
                VerifyDelegateMockAttempt(context, argumentList, arguments);
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
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(methodSymbol => methodSymbol.IsConstructor())
            .ToArray();

        // Bail out early if there are no arguments on constructors or no constructors at all
        (bool IsEmpty, Location Location) constructorIsEmpty = ConstructorIsEmpty(constructors, argumentList, context);
        if (constructorIsEmpty.IsEmpty)
        {
            Diagnostic diagnostic = constructorIsEmpty.Location.CreateDiagnostic(ClassMustHaveMatchingConstructor, argumentList);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // We have constructors, now we need to check if the arguments match any of them
        if (!AnyConstructorsFound(constructors, arguments, context))
        {
            Diagnostic diagnostic = constructorIsEmpty.Location.CreateDiagnostic(ClassMustHaveMatchingConstructor, argumentList);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
