using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock.Get() should not take literals.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MockGetShouldNotTakeLiteralsAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Mock.Get() should not take literals";
    private static readonly LocalizableString Message = "Mock.Get() should not take literal '{0}'";
    private static readonly LocalizableString Description = "Mock.Get() should not take literals.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MockGetShouldNotTakeLiterals,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
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
                string literalText = GetLiteralText(argument.Value);
                context.ReportDiagnostic(argument.Syntax.GetLocation().CreateDiagnostic(Rule, literalText));
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

    private static string GetLiteralText(IOperation operation)
    {
        return operation.Kind switch
        {
            OperationKind.Literal when operation is ILiteralOperation literal => literal.ConstantValue.Value?.ToString() ?? "null",
            OperationKind.DefaultValue when operation is IDefaultValueOperation defaultValue => $"default({defaultValue.Type?.ToDisplayString() ?? string.Empty})",
            OperationKind.Conversion when operation is IConversionOperation conversionOperation => GetLiteralText(conversionOperation.Operand),
            _ => operation.Syntax.ToString(),
        };
    }
}
