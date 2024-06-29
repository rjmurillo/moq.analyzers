namespace Moq.Analyzers.Common;

/// <summary>
/// Base class for an <see cref="DiagnosticAnalyzer"/> which may report only a single <see cref="DiagnosticDescriptor"/>.
/// </summary>
public abstract class SingleDiagnosticAnalyzer : DiagnosticAnalyzerBase
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleDiagnosticAnalyzer"/> class.
    /// </summary>
    /// <param name="rule">A <see cref="DiagnosticDescriptor"/> instance.</param>
    protected SingleDiagnosticAnalyzer(DiagnosticDescriptor rule)
    {
        SupportedDiagnostics = ImmutableArray.Create(rule);
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
}
