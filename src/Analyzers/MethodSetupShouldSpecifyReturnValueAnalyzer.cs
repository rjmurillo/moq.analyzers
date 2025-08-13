using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Method setups that return a value should specify a return value using Returns() or Throws().
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodSetupShouldSpecifyReturnValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Method setup should specify a return value";
    private static readonly LocalizableString Message = "Method setup for '{0}' should specify a return value";
    private static readonly LocalizableString Description = "Method setups that return a value should use Returns() or Throws() to specify a return value.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MethodSetupShouldSpecifyReturnValue,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (!TryGetSetupInvocation(context, out IInvocationOperation? setupInvocation, out _))
        {
            return;
        }

        if (!TryGetMockedMethodWithReturnValue(setupInvocation, out IMethodSymbol? mockedMethod))
        {
            return;
        }

        if (HasReturnValueSpecification(setupInvocation))
        {
            return;
        }

        // Report diagnostic for methods with return types that don't specify a return value
        Diagnostic diagnostic = setupInvocation.Syntax.CreateDiagnostic(Rule, mockedMethod.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool TryGetSetupInvocation(
        OperationAnalysisContext context,
        out IInvocationOperation setupInvocation,
        out MoqKnownSymbols knownSymbols)
    {
        setupInvocation = null!;
        knownSymbols = null!;

        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return false;
        }

        SemanticModel? semanticModel = invocationOperation.SemanticModel;
        if (semanticModel == null)
        {
            return false;
        }

        knownSymbols = new MoqKnownSymbols(semanticModel.Compilation);
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        if (!targetMethod.IsMoqSetupMethod(knownSymbols))
        {
            return false;
        }

        setupInvocation = invocationOperation;
        return true;
    }

    private static bool TryGetMockedMethodWithReturnValue(
        IInvocationOperation setupInvocation,
        out IMethodSymbol mockedMethod)
    {
        mockedMethod = null!;

        ISymbol? mockedMethodSymbol = TryGetMockedMethodSymbol(setupInvocation);
        if (mockedMethodSymbol is not IMethodSymbol method)
        {
            return false;
        }

        if (method.ReturnsVoid)
        {
            return false;
        }

        mockedMethod = method;
        return true;
    }

    /// <summary>
    /// Attempts to resolve the symbol representing the method being referenced in the Setup(...) call.
    /// </summary>
    private static ISymbol? TryGetMockedMethodSymbol(IInvocationOperation moqSetupInvocation) =>
        TryGetSetupArgument(moqSetupInvocation)?.GetReferencedMemberSymbolFromLambda();

    /// <summary>
    /// Extracts the lambda body operation from the first argument of a Moq Setup invocation.
    /// </summary>
    private static IOperation? TryGetSetupArgument(IInvocationOperation moqSetupInvocation)
    {
        if (moqSetupInvocation.Arguments.Length == 0)
        {
            return null;
        }

        IOperation argumentOperation = moqSetupInvocation.Arguments[0].Value;

        // Unwrap conversions (Roslyn often wraps lambdas in IConversionOperation)
        argumentOperation = argumentOperation.WalkDownImplicitConversion();

        return argumentOperation is IAnonymousFunctionOperation lambdaOperation
            ? lambdaOperation.Body
            : null;
    }

    /// <summary>
    /// Checks if the setup invocation is followed by a Returns() or Throws() call.
    /// This uses semantic analysis to verify the method being called.
    /// </summary>
    private static bool HasReturnValueSpecification(IInvocationOperation setupInvocation)
    {
        SyntaxNode setupSyntax = setupInvocation.Syntax;
        SemanticModel? semanticModel = setupInvocation.SemanticModel;

        if (semanticModel == null)
        {
            return false;
        }

        // Check if the setup call is the target of a member access (method chaining)
        if (setupSyntax.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccess)
        {
            // Get the symbol information for the method being accessed
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Name);

            // Check if it's a method call and if the method name is Returns or Throws
            return symbolInfo.Symbol is IMethodSymbol method &&
                   (string.Equals(method.Name, "Returns", StringComparison.Ordinal) || string.Equals(method.Name, "Throws", StringComparison.Ordinal));
        }

        return false;
    }
}
