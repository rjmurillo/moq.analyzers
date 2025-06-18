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
        "SIMPLE001",
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

        // Report any invocation that contains "Mock" or "Of"
        string methodName = invocation.TargetMethod.Name;
        string? typeName = invocation.TargetMethod.ContainingType?.Name;

        if (methodName.Contains("Of") || (typeName?.Contains("Mock") == true))
        {
            context.ReportDiagnostic(invocation.Syntax.GetLocation().CreateDiagnostic(Rule, $"{typeName}.{methodName}"));
        }
    }
}