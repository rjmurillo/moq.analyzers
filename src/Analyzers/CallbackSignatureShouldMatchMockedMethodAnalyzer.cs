using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Callback signature must match the signature of the mocked method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CallbackSignatureShouldMatchMockedMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Bad callback parameters";
    private static readonly LocalizableString Message = "Callback signature for '{0}' must match the signature of the mocked method";
    private static readonly LocalizableString Description = "Callback signature must match the signature of the mocked method.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.BadCallbackParameters,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.BadCallbackParameters}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols),
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        Debug.Assert(context.Operation is IInvocationOperation, "Expected IInvocationOperation");

        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (invocationOperation.Syntax is not InvocationExpressionSyntax callbackOrReturnsInvocation)
        {
            return;
        }

        SemanticModel? semanticModel = invocationOperation.SemanticModel;
        if (semanticModel is null)
        {
            return;
        }

        // Ignoring Callback() and Return() calls without lambda arguments
        if (callbackOrReturnsInvocation.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        if (!semanticModel.IsCallbackOrReturnInvocation(callbackOrReturnsInvocation, knownSymbols))
        {
            return;
        }

        ParenthesizedLambdaExpressionSyntax? callbackLambda = TryGetCallbackLambda(callbackOrReturnsInvocation);
        if (callbackLambda == null)
        {
            return;
        }

        // Ignoring calls with no arguments because those are valid in Moq
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters = callbackLambda.ParameterList.Parameters;
        if (lambdaParameters.Count == 0)
        {
            return;
        }

        InvocationExpressionSyntax? setupInvocation = semanticModel.FindSetupMethodFromCallbackInvocation(knownSymbols, callbackOrReturnsInvocation, context.CancellationToken);

        ValidateCallbackAgainstSetup(context, semanticModel, setupInvocation, callbackLambda, lambdaParameters);
    }

    private static ParenthesizedLambdaExpressionSyntax? TryGetCallbackLambda(InvocationExpressionSyntax callbackOrReturnsInvocation)
    {
        if (callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression is ParenthesizedLambdaExpressionSyntax directLambda)
        {
            return directLambda;
        }

        // Check if this is a delegate constructor callback (e.g., new SomeDelegate(...))
        if (callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression is not ObjectCreationExpressionSyntax delegateConstructor
            || delegateConstructor.ArgumentList?.Arguments.Count <= 0)
        {
            return null;
        }

        // Extract the lambda from the delegate constructor (support both parenthesized and simple lambdas)
        LambdaExpressionSyntax? lambdaExpression = delegateConstructor.ArgumentList!.Arguments[0]?.Expression as LambdaExpressionSyntax;

        // Simple lambdas are currently skipped to avoid handling edge cases and maintain simplicity.
        // TODO: Implement support for SimpleLambdaExpressionSyntax in delegate constructors.
        if (lambdaExpression is SimpleLambdaExpressionSyntax)
        {
            return null;
        }

        return lambdaExpression as ParenthesizedLambdaExpressionSyntax;
    }

    private static void ValidateCallbackAgainstSetup(
        OperationAnalysisContext context,
        SemanticModel semanticModel,
        InvocationExpressionSyntax? setupInvocation,
        ParenthesizedLambdaExpressionSyntax callbackLambda,
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters)
    {
        InvocationExpressionSyntax? mockedMethodInvocation = setupInvocation.FindMockedMethodInvocationFromSetupMethod();
        if (mockedMethodInvocation == null)
        {
            return;
        }

        string methodName = GetMethodName(mockedMethodInvocation);
        SeparatedSyntaxList<ArgumentSyntax> mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

        if (mockedMethodArguments.Count != lambdaParameters.Count)
        {
            Diagnostic diagnostic = callbackLambda.ParameterList.CreateDiagnostic(Rule, methodName);
            context.ReportDiagnostic(diagnostic);
        }
        else
        {
            IEnumerable<IMethodSymbol> mockedMethodSymbols = semanticModel.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(setupInvocation);
            ValidateParameters(context, semanticModel, mockedMethodSymbols, lambdaParameters, methodName);
        }
    }

    private static void ValidateParameters(
        OperationAnalysisContext context,
        SemanticModel semanticModel,
        IEnumerable<IMethodSymbol> mockedMethodSymbols,
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters,
        string methodName)
    {
        foreach (IMethodSymbol mockedMethod in mockedMethodSymbols)
        {
            if (ParametersMatch(semanticModel, mockedMethod, lambdaParameters, context.CancellationToken))
            {
                return;
            }
        }

        if (lambdaParameters.Count > 0)
        {
            Diagnostic diagnostic = lambdaParameters[0].CreateDiagnostic(Rule, methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool ParametersMatch(
        SemanticModel semanticModel,
        IMethodSymbol mockedMethod,
        SeparatedSyntaxList<ParameterSyntax> lambdaParameters,
        CancellationToken cancellationToken)
    {
        if (mockedMethod.Parameters.Length != lambdaParameters.Count)
        {
            return false;
        }

        for (int parameterIndex = 0; parameterIndex < lambdaParameters.Count; parameterIndex++)
        {
            IParameterSymbol mockedMethodParameter = mockedMethod.Parameters[parameterIndex];
            ParameterSyntax lambdaParameter = lambdaParameters[parameterIndex];

            if (!ParameterTypesMatch(semanticModel, mockedMethodParameter, lambdaParameter, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParameterTypesMatch(
        SemanticModel semanticModel,
        IParameterSymbol mockedParameter,
        ParameterSyntax lambdaParameter,
        CancellationToken cancellationToken)
    {
        TypeSyntax? lambdaParameterTypeSyntax = lambdaParameter.Type;
        if (lambdaParameterTypeSyntax is null)
        {
            return true; // Can't validate, assume ok
        }

        TypeInfo lambdaParameterType = semanticModel.GetTypeInfo(lambdaParameterTypeSyntax, cancellationToken);
        ITypeSymbol? lambdaParameterTypeSymbol = lambdaParameterType.Type;

        if (lambdaParameterTypeSymbol is null)
        {
            return true; // Can't validate, assume ok
        }

        ITypeSymbol mockedParameterType = mockedParameter.Type;

        if (!semanticModel.HasConversion(mockedParameterType, lambdaParameterTypeSymbol))
        {
            return false;
        }

        return mockedParameter.RefKind == GetRefKind(lambdaParameter);
    }

    private static RefKind GetRefKind(ParameterSyntax parameter)
    {
        if (parameter.Modifiers.Count == 0)
        {
            return RefKind.None;
        }

        return parameter.Modifiers[0].ValueText switch
        {
            "ref" => RefKind.Ref,
            "out" => RefKind.Out,
            "in" => RefKind.In,
            _ => RefKind.None,
        };
    }

    private static string GetMethodName(InvocationExpressionSyntax mockedMethodInvocation)
    {
        return mockedMethodInvocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => "Unknown",
        };
    }
}
