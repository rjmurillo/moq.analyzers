namespace Moq.Analyzers.Common;

internal static class CompilationOptionsExtensions
{
    /// <summary>
    /// Determines if the diagnostic identified by the given identifier is currently suppressed.
    /// </summary>
    /// <param name="compilationOptions">The compilation options that will be used to determine if the diagnostic is currently suppressed.</param>
    /// <param name="descriptor">The diagnostic descriptor to check.</param>
    /// <returns>True if the diagnostic is currently suppressed.</returns>
    internal static bool IsAnalyzerSuppressed(this CompilationOptions compilationOptions, DiagnosticDescriptor descriptor)
    {
        switch (descriptor.GetEffectiveSeverity(compilationOptions))
        {
            case ReportDiagnostic.Suppress:
                return true;
            default:
                return false;
        }
    }
}
