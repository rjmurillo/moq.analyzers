using Microsoft.CodeAnalysis.Diagnostics;

namespace Moq.Analyzers.Benchmarks.Helpers;

internal static class AnalysisResultExtensions
{
    public static AnalysisResult AssertValidAnalysisResult(this AnalysisResult analysisResult)
    {
        if (analysisResult.Analyzers.Length != 1)
        {
            throw new InvalidOperationException($"Expected a single analyzer but found '{analysisResult.Analyzers.Length}'");
        }

        if (analysisResult.CompilationDiagnostics.Count != 0)
        {
            throw new InvalidOperationException($"Expected no compilation diagnostics but found '{analysisResult.CompilationDiagnostics.Count}'");
        }

        return analysisResult;
    }
}
