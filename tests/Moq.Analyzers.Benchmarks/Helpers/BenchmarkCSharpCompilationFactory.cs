using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Benchmarks.Helpers;

internal static class BenchmarkCSharpCompilationFactory
{
    public static async Task<(CompilationWithAnalyzers Baseline, CompilationWithAnalyzers Test)> CreateAsync<TAnalyzer>(
        (string Name, string Contents)[] sources,
        AnalyzerOptions? options = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
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

    public static async Task<(CompilationWithAnalyzers Baseline, CompilationWithAnalyzers Test1, CompilationWithAnalyzers Test2)> CreateAsync<TAnalyzer1, TAnalyzer2>(
        (string Name, string Contents)[] sources,
        AnalyzerOptions? options = null)
        where TAnalyzer1 : DiagnosticAnalyzer, new()
        where TAnalyzer2 : DiagnosticAnalyzer, new()
    {
        Compilation? compilation = await CSharpCompilationCreator.CreateAsync(sources).ConfigureAwait(false);

        if (compilation is null)
        {
            throw new InvalidOperationException("Failed to create compilation");
        }

        CompilationWithAnalyzers baseline = compilation.WithAnalyzers([new EmptyDiagnosticAnalyzer()], options, CancellationToken.None);
        CompilationWithAnalyzers test1 = compilation.WithAnalyzers([new TAnalyzer1()], options, CancellationToken.None);
        CompilationWithAnalyzers test2 = compilation.WithAnalyzers([new TAnalyzer2()], options, CancellationToken.None);

        return (baseline, test1, test2);
    }
}

