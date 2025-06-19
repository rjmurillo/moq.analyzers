using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Simple analyzer to detect ANY invocation operations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SimpleInvocationAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Simple invocation detected";
    private static readonly LocalizableString Message = "Simple invocation detected: {0}";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SimpleInvocation,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(context.Operation.SemanticModel!.Compilation);

        // Report any invocation that matches Mock.Of
        IMethodSymbol targetMethod = invocation.TargetMethod;

        if (targetMethod.ContainingType is not null &&
            targetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default) &&
            string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal))
        {
            context.ReportDiagnostic(invocation.Syntax.GetLocation().CreateDiagnostic(Rule, $"{targetMethod.ContainingType.Name}.{targetMethod.Name}"));
        }
    }
}
