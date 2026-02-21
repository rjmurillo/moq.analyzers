using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Method setups that return a value should specify a return value using
/// Returns(), ReturnsAsync(), Throws(), or ThrowsAsync().
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodSetupShouldSpecifyReturnValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Method setup should specify a return value";
    private static readonly LocalizableString Message = "Method setup for '{0}' should specify a return value";
    private static readonly LocalizableString Description = "Method setups that return a value should use Returns(), ReturnsAsync(), Throws(), or ThrowsAsync() to specify a return value.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MethodSetupShouldSpecifyReturnValue,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.MethodSetupShouldSpecifyReturnValue}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (!TryGetSetupInvocation(context, out IInvocationOperation? setupInvocation, out MoqKnownSymbols? knownSymbols))
        {
            return;
        }

        if (!TryGetMockedMethodWithReturnValue(setupInvocation, out IMethodSymbol? mockedMethod))
        {
            return;
        }

        if (HasReturnValueSpecification(setupInvocation, knownSymbols, context.CancellationToken))
        {
            return;
        }

        Diagnostic diagnostic = setupInvocation.Syntax.CreateDiagnostic(Rule, mockedMethod.ToDisplayString());
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
    /// Checks if the setup invocation is followed by a return value specification
    /// anywhere in the method chain (e.g., .Setup().Callback().Returns()).
    /// Uses semantic analysis to resolve method symbols, including candidate symbols
    /// when overload resolution fails.
    /// </summary>
    private static bool HasReturnValueSpecification(
        IInvocationOperation setupInvocation,
        MoqKnownSymbols knownSymbols,
        CancellationToken cancellationToken)
    {
        SyntaxNode setupSyntax = setupInvocation.Syntax;
        SemanticModel? semanticModel = setupInvocation.SemanticModel;

        if (semanticModel == null)
        {
            return false;
        }

        if (setupSyntax is not ExpressionSyntax expressionSyntax)
        {
            Debug.Assert(false, "IInvocationOperation.Syntax should always be an ExpressionSyntax");
            return false;
        }

        ExpressionSyntax? current = expressionSyntax;
        while (current?.WalkUpParentheses()?.Parent is MemberAccessExpressionSyntax memberAccess)
        {
            cancellationToken.ThrowIfCancellationRequested();

            InvocationExpressionSyntax? invocation = memberAccess.Parent as InvocationExpressionSyntax;
            SymbolInfo symbolInfo = invocation != null
                ? semanticModel.GetSymbolInfo(invocation, cancellationToken)
                : semanticModel.GetSymbolInfo(memberAccess, cancellationToken);

            // First try semantic symbol matching (exact). Then fall back to method
            // name matching when Roslyn resolves a symbol that IsInstanceOf cannot
            // match (e.g., delegate-based overloads with constructed generic types).
            // The name-based fallback is safe because this walk only visits methods
            // chained from a verified Setup() call, and we verify the named method
            // exists in the Moq compilation.
            if (HasReturnValueSymbol(symbolInfo, knownSymbols)
                || IsKnownReturnValueMethodName(memberAccess.Name.Identifier.ValueText, knownSymbols))
            {
                return true;
            }

            current = memberAccess.Parent as InvocationExpressionSyntax;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a method name corresponds to a known Moq return value specification
    /// method that exists in the compilation. Used as a last-resort fallback when Roslyn
    /// cannot resolve symbols at all (e.g., delegate-based overloads with failed type inference).
    /// </summary>
    private static bool IsKnownReturnValueMethodName(string methodName, MoqKnownSymbols knownSymbols)
    {
        return methodName switch
        {
            "Returns" => !knownSymbols.IReturnsReturns.IsEmpty
                         || !knownSymbols.IReturns1Returns.IsEmpty
                         || !knownSymbols.IReturns2Returns.IsEmpty,
            "ReturnsAsync" => !knownSymbols.ReturnsExtensionsReturnsAsync.IsEmpty,
            "Throws" => !knownSymbols.IThrowsThrows.IsEmpty,
            "ThrowsAsync" => !knownSymbols.ReturnsExtensionsThrowsAsync.IsEmpty,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether the given <see cref="SymbolInfo"/> resolves to a Moq return value
    /// specification method. Checks the resolved symbol first, then falls back to scanning
    /// candidate symbols when Roslyn cannot complete overload resolution.
    /// </summary>
    private static bool HasReturnValueSymbol(SymbolInfo symbolInfo, MoqKnownSymbols knownSymbols)
    {
        if (symbolInfo.Symbol is IMethodSymbol resolved)
        {
            return resolved.IsMoqReturnValueSpecificationMethod(knownSymbols);
        }

        return symbolInfo.CandidateSymbols
            .OfType<IMethodSymbol>()
            .Any(method => method.IsMoqReturnValueSpecificationMethod(knownSymbols));
    }
}
