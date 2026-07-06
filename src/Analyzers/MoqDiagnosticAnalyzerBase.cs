namespace Moq.Analyzers;

/// <summary>
/// Provides the common Roslyn analyzer initialization and Moq compilation-start gate.
/// </summary>
public abstract class MoqDiagnosticAnalyzerBase : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public sealed override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private protected abstract void RegisterCompilationActions(CompilationStartAnalysisContext context, MoqKnownSymbols knownSymbols);

    private void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        RegisterCompilationActions(context, knownSymbols);
    }
}
