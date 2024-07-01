namespace Moq.Analyzers.Common;

/// <summary>
/// Base class for <see cref="DiagnosticAnalyzer"/>.
/// </summary>
public abstract class DiagnosticAnalyzerBase : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public sealed override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GetGeneratedCodeAnalysisFlags());
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    /// <summary>
    /// An action to be executed at compilation start. A compilation start action can register
    /// other actions and/or collect state information to be used in diagnostic analysis.
    /// </summary>
    /// <param name="context">Context for a compilation start action.</param>
    protected abstract void RegisterCompilationStartAction(CompilationStartAnalysisContext context);

    /// <summary>
    /// Gets the generated code analysis flags.
    /// </summary>
    /// <returns>Default value is <see cref="GeneratedCodeAnalysisFlags.None"/>.</returns>
    protected virtual GeneratedCodeAnalysisFlags GetGeneratedCodeAnalysisFlags()
    {
        return GeneratedCodeAnalysisFlags.None;
    }
}
