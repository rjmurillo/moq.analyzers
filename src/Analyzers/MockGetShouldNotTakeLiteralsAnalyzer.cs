using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock.Get() should not take literals.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MockGetShouldNotTakeLiteralsAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Mock.Get() should not take literals";
    private static readonly LocalizableString Message = "Mock.Get() should not take literals";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MockGetShouldNotTakeLiterals,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.MockGetShouldNotTakeLiterals}.md");

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

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Look for the Mock.Get() method
        ImmutableArray<IMethodSymbol> getMethods = knownSymbols.MockGet;
        if (getMethods.IsEmpty)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, getMethods),
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> wellKnownGetMethods)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!targetMethod.IsInstanceOf(wellKnownGetMethods))
        {
            return;
        }

        // Check if any argument is a literal
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            if (IsLiteralValue(argument.Value))
            {
                context.ReportDiagnostic(argument.Syntax.GetLocation().CreateDiagnostic(Rule));
            }
        }
    }

    private static bool IsLiteralValue(IOperation operation)
    {
        return operation.Kind switch
        {
            OperationKind.Literal => true,
            OperationKind.DefaultValue => true,
            OperationKind.Conversion when operation is IConversionOperation conversionOperation => IsLiteralValue(conversionOperation.Operand),
            _ => false,
        };
    }
}
