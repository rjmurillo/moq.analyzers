using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
[BenchmarkCategory("Moq1301")]
public class Moq1301MockGetLiteralsBenchmarks
{
#pragma warning disable ECS0900
    [Params(1, 1_000)]
#pragma warning restore ECS0900
    public int FileCount { get; set; }

    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private static CompilationWithAnalyzers? TestCompilation { get; set; }

    /// <summary>
    /// Prepares Roslyn compilation objects with generated source files for benchmarking the Moq1301 analyzer.
    /// </summary>
    /// <remarks>
    /// Generates a number of source files specified by <c>FileCount</c>, each containing code that triggers or does not trigger the analyzer. 
    /// Initializes both baseline and test compilations with these sources and the appropriate reference assemblies.
    /// </remarks>
    [GlobalSetup]
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Async setup not supported in BenchmarkDotNet.See https://github.com/dotnet/BenchmarkDotNet/issues/2442.")]
    public void SetupCompilation()
    {
        List<(string Name, string Content)> sources = [];
        for (int index = 0; index < FileCount; index++)
        {
            string name = "TypeName" + index;
            sources.Add((name, @$"
using System;
using Moq;

public interface ISample{index}
{{
    int Calculate(int a, int b);
}}

internal class {name}
{{
    private void Test()
    {{
        Mock.Get<string>(""literal string""); // This should trigger Moq1301
        _ = ""sample test""; // Add an expression that looks similar but does not match
    }}
}}
"));
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies("Net80WithOldMoq");
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory.CreateAsync<MockGetShouldNotTakeLiteralsAnalyzer>(sources.ToArray(), referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Benchmarks the analyzer by verifying that the expected number of diagnostics are reported for the generated source files.
    /// </summary>
    /// <remarks>
    /// Throws an <see cref="InvalidOperationException"/> if the number of diagnostics does not match <c>FileCount</c>.
    /// </remarks>
    [Benchmark]
    public async Task Moq1301WithDiagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics =
            (await TestCompilation!
            .GetAnalysisResultAsync(CancellationToken.None)
            .ConfigureAwait(false))
            .AssertValidAnalysisResult()
            .GetAllDiagnostics();

        if (diagnostics.Length != FileCount)
        {
            throw new InvalidOperationException($"Expected '{FileCount:N0}' analyzer diagnostics but found '{diagnostics.Length}'");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Moq1301Baseline()
    {
        ImmutableArray<Diagnostic> diagnostics =
            (await BaselineCompilation!
            .GetAnalysisResultAsync(CancellationToken.None)
            .ConfigureAwait(false))
            .AssertValidAnalysisResult()
            .GetAllDiagnostics();

        if (diagnostics.Length != 0)
        {
            throw new InvalidOperationException($"Expected no analyzer diagnostics but found '{diagnostics.Length}'");
        }
    }
}
