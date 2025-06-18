namespace Moq.Analyzers;

/// <summary>
/// Callback signature must match the signature of the mocked method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CallbackSignatureShouldMatchMockedMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Bad callback parameters";
    private static readonly LocalizableString Message = "Callback signature must match the signature of the mocked method";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.BadCallbackParameters,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.BadCallbackParameters}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);

        InvocationExpressionSyntax callbackOrReturnsInvocation = (InvocationExpressionSyntax)context.Node;

        SeparatedSyntaxList<ArgumentSyntax> callbackOrReturnsMethodArguments = callbackOrReturnsInvocation.ArgumentList.Arguments;

        // Ignoring Callback() and Return() calls without lambda arguments
        if (callbackOrReturnsMethodArguments.Count == 0) return;

        if (!context.SemanticModel.IsCallbackOrReturnInvocation(callbackOrReturnsInvocation)) return;

        ParenthesizedLambdaExpressionSyntax? callbackLambda = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;

        // Check if this is a delegate constructor callback (e.g., new SomeDelegate(...))
        if (callbackLambda == null)
        {
            ObjectCreationExpressionSyntax? delegateConstructor = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ObjectCreationExpressionSyntax;
            if (delegateConstructor?.ArgumentList?.Arguments.Count > 0)
            {
                // Extract the lambda from the delegate constructor
                callbackLambda = delegateConstructor.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;
            }
        }

        // Ignoring callbacks without lambda
        if (callbackLambda == null) return;

        // Ignoring calls with no arguments because those are valid in Moq
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters = callbackLambda.ParameterList.Parameters;
        if (lambdaParameters.Count == 0) return;

        InvocationExpressionSyntax? setupInvocation = context.SemanticModel.FindSetupMethodFromCallbackInvocation(knownSymbols, callbackOrReturnsInvocation, context.CancellationToken);
        InvocationExpressionSyntax? mockedMethodInvocation = setupInvocation.FindMockedMethodInvocationFromSetupMethod();
        if (mockedMethodInvocation == null) return;

        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

        if (mockedMethodArguments.Count != lambdaParameters.Count)
        {
            Diagnostic diagnostic = callbackLambda.ParameterList.CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            // Get the actual mocked method symbols to access parameter information including ref/out/in modifiers
            IEnumerable<IMethodSymbol> mockedMethodSymbols = context.SemanticModel.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(setupInvocation);
            ValidateParameters(context, mockedMethodSymbols, lambdaParameters);
        }
    }

    private static void ValidateParameters(
        SyntaxNodeAnalysisContext context,
        IEnumerable<IMethodSymbol> mockedMethodSymbols,
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters)
    {
        // Check if the lambda parameters match any of the mocked method overloads
        foreach (IMethodSymbol mockedMethod in mockedMethodSymbols)
        {
            if (ParametersMatch(context, mockedMethod, lambdaParameters))
            {
                // Found a matching overload, no diagnostic needed
                return;
            }
        }

        // No matching overload found, report diagnostic on the first parameter
        if (lambdaParameters.Count > 0)
        {
            Diagnostic diagnostic = lambdaParameters[0].CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool ParametersMatch(SyntaxNodeAnalysisContext context, IMethodSymbol mockedMethod, SeparatedSyntaxList<ParameterSyntax> lambdaParameters)
    {
        if (mockedMethod.Parameters.Length != lambdaParameters.Count)
        {
            return false;
        }

        for (int parameterIndex = 0; parameterIndex < lambdaParameters.Count; parameterIndex++)
        {
            IParameterSymbol mockedMethodParameter = mockedMethod.Parameters[parameterIndex];
            ParameterSyntax lambdaParameter = lambdaParameters[parameterIndex];

            if (!ParameterTypesMatch(context, mockedMethodParameter, lambdaParameter))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParameterTypesMatch(SyntaxNodeAnalysisContext context, IParameterSymbol mockedParameter, ParameterSyntax lambdaParameter)
    {
        TypeSyntax? lambdaParameterTypeSyntax = lambdaParameter.Type;
        if (lambdaParameterTypeSyntax is null) return true; // Can't validate, assume ok

        TypeInfo lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameterTypeSyntax, context.CancellationToken);
        ITypeSymbol? lambdaParameterTypeSymbol = lambdaParameterType.Type;

        if (lambdaParameterTypeSymbol is null) return true; // Can't validate, assume ok

        // Get the underlying type for the mocked parameter (without ref/out/in modifiers)
        ITypeSymbol mockedParameterType = mockedParameter.Type;

        // Check if the basic types match (allowing for conversions)
        if (!HasConversion(context.SemanticModel, mockedParameterType, lambdaParameterTypeSymbol))
        {
            return false;
        }

        // Check ref/out/in modifiers
        RefKind mockedRefKind = mockedParameter.RefKind;
        RefKind lambdaRefKind = GetRefKind(lambdaParameter);

        return mockedRefKind == lambdaRefKind;
    }

    private static RefKind GetRefKind(ParameterSyntax parameter)
    {
        if (parameter.Modifiers.Count == 0)
        {
            return RefKind.None;
        }

        string? firstModifierText = parameter.Modifiers[0].ValueText;

        return firstModifierText switch
        {
            "ref" => RefKind.Ref,
            "out" => RefKind.Out,
            "in" => RefKind.In,
            _ => RefKind.None,
        };
    }

    private static bool HasConversion(SemanticModel semanticModel, ITypeSymbol source, ITypeSymbol destination)
    {
        // This condition checks whether a valid type conversion exists between the parameter in the mocked method
        // and the corresponding parameter in the callback lambda expression.
        //
        // - `conversion.Exists` checks if there is any type conversion possible between the two types
        //
        // The second part ensures that the conversion is either:
        // 1. an implicit conversion,
        // 2. an identity conversion (meaning the types are exactly the same), or
        // 3. an explicit conversion.
        //
        // If the conversion exists, and it is one of these types (implicit, identity, or explicit), the analyzer will
        // skip the diagnostic check, as the callback parameter type is considered compatible with the mocked method's
        // parameter type.
        //
        // There are circumstances where the syntax tree will present an item with an explicit conversion, but the
        // ITypeSymbol instance passed in here is reduced to the same type. For example, we have a test that has
        // an explicit conversion operator from a string to a custom type. That is presented here as two instances
        // of CustomType, which is an implicit identity conversion, not an explicit
        Conversion conversion = semanticModel.Compilation.ClassifyConversion(source, destination);

        return conversion.Exists && (conversion.IsImplicit || conversion.IsExplicit || conversion.IsIdentity);
    }
}
