using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Benchmarks.Helpers;

internal static class BenchmarkCSharpCompilationCreator<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static async Task<(CompilationWithAnalyzers Baseline, CompilationWithAnalyzers Test)> CreateAsync(
        (string Name, string Contents)[] sources,
        AnalyzerOptions? options = null)
    {
        Compilation? compilation = await CSharpCompilationCreator.CreateAsync(sources).ConfigureAwait(false);

        if (compilation is null)
        {
            throw new InvalidOperationException("Failed to create compilation");
        }

        CompilationWithAnalyzers baseline = compilation.WithAnalyzers([new EmptyDiagnosticAnalyzer()], options, CancellationToken.None);
        CompilationWithAnalyzers test = compilation.WithAnalyzers([new TAnalyzer()], options, CancellationToken.None);

        return (baseline, test);
    }
}
