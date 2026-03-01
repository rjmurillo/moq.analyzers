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

        ISymbol targetSymbol = symbolInfo.Symbol;
        if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
        {
            targetSymbol = parameterSymbol.Type;
        }
        else if (symbolInfo.Symbol is ILocalSymbol localSymbol)
        {
            targetSymbol = localSymbol.Type;
        }
        else if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
        {
            targetSymbol = fieldSymbol.Type;
        }

        return targetSymbol.IsInstanceOf(knownSymbols.MockBehavior);
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
        bool hasReturnedMock = true;
        bool hasMockBehavior = true;
        SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax, context.CancellationToken);

        if (symbol.Symbol is not IMethodSymbol method)
        {
            return;
        }

        if (!method.IsInstanceOf(knownSymbols.MockOf) && !method.IsInstanceOf(knownSymbols.MockRepositoryCreate))
        {
            return;
        }

        // We are calling MockRepository.Create<T> or Mock.Of<T>, determine which
        ArgumentListSyntax? argumentList = null;
        if (method.IsInstanceOf(knownSymbols.MockOf))
        {
            // Mock.Of<T> can specify condition for construction and MockBehavior, but
            // cannot specify constructor parameters
            //
            // The only parameters that can be passed are not relevant for verification
            // to just strip them
            hasReturnedMock = false;
        }
        else
        {
            argumentList = invocationExpressionSyntax.ArgumentList;
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

        VerifyMockAttempt(context, knownSymbols, returnType, argumentList, hasMockBehavior);
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
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "This should be refactored; suppressing for now to enable TreatWarningsAsErrors in CI.")]
    private static bool? AnyConstructorsFound(
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

        // Special case for Lambda expression syntax
        // In Moq you can specify a Lambda expression that creates an instance
        // of the specified type
        // See https://github.com/devlooped/moq/blob/18dc7410ad4f993ce0edd809c5dfcaa3199f13ff/src/Moq/Mock%601.cs#L200
        //
        // The parenthesized lambda takes arguments as the first child node
        // which may be empty or have args defined as part of a closure.
        // Either way, we don't care about that, we only care that the
        // constructor is valid.
        //
        // Since this does not use reflection through Castle, an invalid
        // lambda here would cause the compiler to break, so no need to
        // do additional checks.
        if (arguments.Length == 1 && arguments[0].Expression.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
        {
            return null;
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
