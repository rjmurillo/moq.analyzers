using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
[BenchmarkCategory("Moq1400")]
public class Moq1400ExplicitBehaviorBenchmarks
{
#pragma warning disable ECS0900
    [Params(1, 1_000)]
#pragma warning restore ECS0900
    public int FileCount { get; set; }

    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private static CompilationWithAnalyzers? TestCompilation { get; set; }

    /// <summary>
    /// Prepares Roslyn compilations with generated source files and analyzers for benchmarking, based on the current <see cref="FileCount"/>.
    /// </summary>
    /// <remarks>
    /// Generates the specified number of source files, each containing a unique interface and class with mock usage, and creates two compilations: one with the analyzer under test and one baseline without diagnostics.
    /// </remarks>
    [IterationSetup]
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
    string Name {{ get; set; }}
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<ISample{index}>();
        _ = ""sample test""; // Add an expression that looks similar but does not match
    }}
}}
"));
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies("Net80WithOldMoq");
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory.CreateAsync<SetExplicitMockBehaviorAnalyzer>(sources.ToArray(), referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Benchmarks the analyzer by verifying that the test compilation produces exactly one diagnostic per generated source file.
    /// </summary>
    /// <remarks>
    /// Throws an <see cref="InvalidOperationException"/> if the number of diagnostics does not match <c>FileCount</c>.
    /// </remarks>
    [Benchmark]
    public async Task Moq1400WithDiagnostics()
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
    public async Task Moq1400Baseline()
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
