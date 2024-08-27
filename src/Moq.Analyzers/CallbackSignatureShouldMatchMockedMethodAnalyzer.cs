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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax callbackOrReturnsInvocation = (InvocationExpressionSyntax)context.Node;

        SeparatedSyntaxList<ArgumentSyntax> callbackOrReturnsMethodArguments = callbackOrReturnsInvocation.ArgumentList.Arguments;

        // Ignoring Callback() and Return() calls without lambda arguments
        if (callbackOrReturnsMethodArguments.Count == 0) return;

        if (!context.SemanticModel.IsCallbackOrReturnInvocation(callbackOrReturnsInvocation)) return;

        ParenthesizedLambdaExpressionSyntax? callbackLambda = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;

        // Ignoring callbacks without lambda
        if (callbackLambda == null) return;

        // Ignoring calls with no arguments because those are valid in Moq
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters = callbackLambda.ParameterList.Parameters;
        if (lambdaParameters.Count == 0) return;

        InvocationExpressionSyntax? setupInvocation = context.SemanticModel.FindSetupMethodFromCallbackInvocation(callbackOrReturnsInvocation, context.CancellationToken);
        InvocationExpressionSyntax? mockedMethodInvocation = setupInvocation.FindMockedMethodInvocationFromSetupMethod();
        if (mockedMethodInvocation == null) return;

        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

        if (mockedMethodArguments.Count != lambdaParameters.Count)
        {
            Diagnostic diagnostic = callbackLambda.ParameterList.GetLocation().CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            ValidateParameters(context, mockedMethodArguments, lambdaParameters);
        }
    }

    private static void ValidateParameters(
        SyntaxNodeAnalysisContext context,
        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments,
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters)
    {
        for (int argumentIndex = 0; argumentIndex < mockedMethodArguments.Count; argumentIndex++)
        {
            TypeSyntax? lambdaParameterTypeSyntax = lambdaParameters[argumentIndex].Type;

            // We're unable to get the type from the Syntax Tree, so abort the type checking because something else
            // is happening (e.g., we're compiling on partial code) and we need the type to do the additional checks.
            if (lambdaParameterTypeSyntax is null)
            {
                continue;
            }

            TypeInfo lambdaParameterType = context.SemanticModel.GetTypeInfo(
                lambdaParameterTypeSyntax,
                context.CancellationToken);
            TypeInfo mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(
                mockedMethodArguments[argumentIndex].Expression,
                context.CancellationToken);

            ITypeSymbol? lambdaParameterTypeSymbol = lambdaParameterType.Type;
            ITypeSymbol? mockedMethodTypeSymbol = mockedMethodArgumentType.Type;

            if (lambdaParameterTypeSymbol is null || mockedMethodTypeSymbol is null)
            {
                continue;
            }

            if (!HasUserDefinedConversion(context.SemanticModel, mockedMethodTypeSymbol, lambdaParameterTypeSymbol))
            {
                Diagnostic diagnostic = lambdaParameters[argumentIndex].GetLocation().CreateDiagnostic(Rule);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool HasUserDefinedConversion(SemanticModel semanticModel, ITypeSymbol source, ITypeSymbol destination)
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
        Conversion conversion = semanticModel.Compilation.ClassifyConversion(source, destination);
        bool conversionExists = conversion.Exists && (conversion.IsImplicit || conversion.IsExplicit || conversion.IsIdentity);

        switch (conversionExists)
        {
            case true:
                return true;
            // Fall back to seeking an implicit conversion operator
            // The Conversion type will not show a user defined implicit conversion, so we have to seek it out explicitly
            case false:
                {
                    foreach (ISymbol? member in destination.GetMembers())
                    {
                        // Check for implicit conversion operator
                        if (member is not IMethodSymbol { MethodKind: MethodKind.Conversion } method
                            || !method.ReturnType.Equals(destination, SymbolEqualityComparer.IncludeNullability))
                        {
                            continue;
                        }

                        // Implicit conversion
                        if (method is { IsImplicitlyDeclared: true, Parameters.Length: 1 } && method.Parameters[0].Type.Equals(source, SymbolEqualityComparer.IncludeNullability))
                        {
                            return true;
                        }
                    }

                    break;
                }
        }

        return false;
    }
}
