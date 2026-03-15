using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Protected member setups using string-based overloads must use ItExpr matchers, not It matchers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ProtectedSetupShouldUseItExprAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Protected setup should use ItExpr";
    private static readonly LocalizableString Message = "Protected member setup uses 'It.{0}' which is not compatible with string-based overloads; use an ItExpr matcher instead";
    private static readonly LocalizableString Description = "Protected member setups using string-based overloads must use ItExpr matchers instead of It matchers.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ProtectedSetupUsesItMatcherInsteadOfItExpr,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.ProtectedSetupUsesItMatcherInsteadOfItExpr}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Require both Protected API types and It type to be available
        if (knownSymbols.IProtectedMock1 is null || knownSymbols.It is null)
        {
            return;
        }

        // Collect all string-based protected Setup/Verify methods
        ImmutableArray<IMethodSymbol> protectedStringMethods = BuildProtectedStringMethods(knownSymbols);

        if (protectedStringMethods.IsEmpty)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols, protectedStringMethods),
            OperationKind.Invocation);
    }

    private static ImmutableArray<IMethodSymbol> BuildProtectedStringMethods(MoqKnownSymbols knownSymbols)
    {
        ImmutableArray<IMethodSymbol>.Builder builder = ImmutableArray.CreateBuilder<IMethodSymbol>();
        AddStringOverloads(builder, knownSymbols.IProtectedMock1Setup);
        AddStringOverloads(builder, knownSymbols.IProtectedMock1SetupSet);
        AddStringOverloads(builder, knownSymbols.IProtectedMock1SetupSequence);
        AddStringOverloads(builder, knownSymbols.IProtectedMock1Verify);
        AddStringOverloads(builder, knownSymbols.IProtectedMock1VerifySet);
        return builder.ToImmutable();
    }

    private static void AddStringOverloads(ImmutableArray<IMethodSymbol>.Builder builder, ImmutableArray<IMethodSymbol> methods)
    {
        foreach (IMethodSymbol method in methods)
        {
            if (HasStringFirstParameter(method))
            {
                builder.Add(method);
            }
        }
    }

    private static bool HasStringFirstParameter(IMethodSymbol method)
    {
        if (method.Parameters.IsEmpty)
        {
            return false;
        }

        return method.Parameters[0].Type.SpecialType == SpecialType.System_String;
    }

    private static void Analyze(
        OperationAnalysisContext context,
        MoqKnownSymbols knownSymbols,
        ImmutableArray<IMethodSymbol> protectedStringMethods)
    {
        Debug.Assert(context.Operation is IInvocationOperation, "Expected IInvocationOperation");

        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!targetMethod.IsInstanceOf(protectedStringMethods))
        {
            return;
        }

        // Scan all arguments for It.* matcher usage
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            ScanForItMatchers(context, argument.Value, knownSymbols);
        }
    }

    private static void ScanForItMatchers(
        OperationAnalysisContext context,
        IOperation operation,
        MoqKnownSymbols knownSymbols)
    {
        // Unwrap all conversions (both implicit and explicit) to handle casts like (object)It.IsAny<string>()
        IOperation current = operation.WalkDownConversion();

        // Direct It.* call as argument
        if (current is IInvocationOperation matcherInvocation)
        {
            ReportIfItMatcher(context, matcherInvocation, knownSymbols);
            return;
        }

        // params array: It.* calls appear inside array creation
        if (current is IArrayCreationOperation arrayCreation)
        {
            ScanArrayInitializer(context, arrayCreation, knownSymbols);
        }
    }

    private static void ScanArrayInitializer(
        OperationAnalysisContext context,
        IArrayCreationOperation arrayCreation,
        MoqKnownSymbols knownSymbols)
    {
        if (arrayCreation.Initializer is null)
        {
            return;
        }

        foreach (IOperation element in arrayCreation.Initializer.ElementValues)
        {
            IOperation unwrapped = element.WalkDownConversion();

            if (unwrapped is IInvocationOperation elementInvocation)
            {
                ReportIfItMatcher(context, elementInvocation, knownSymbols);
            }
        }
    }

    private static void ReportIfItMatcher(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        MoqKnownSymbols knownSymbols)
    {
        IMethodSymbol matcherMethod = invocation.TargetMethod;
        INamedTypeSymbol? containingType = matcherMethod.ContainingType;

        if (containingType is null)
        {
            return;
        }

        // Check if the containing type is Moq.It (not Moq.Protected.ItExpr).
        // TODO: It.Ref<T>.IsAny is a nested type (Moq.It+Ref<T>) with a different containing
        // type symbol than Moq.It, so this check does not detect it. Track in a future issue.
        if (!SymbolEqualityComparer.Default.Equals(containingType, knownSymbols.It))
        {
            return;
        }

        string itMethodName = matcherMethod.Name;

        context.ReportDiagnostic(
            invocation.Syntax.GetLocation().CreateDiagnostic(Rule, itMethodName));
    }
}
