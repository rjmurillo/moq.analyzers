using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
[BenchmarkCategory("Moq1100")]
public class Moq1100CallbackBenchmarks
{
#pragma warning disable ECS0900
    [Params(1, 1_000)]
#pragma warning restore ECS0900
    public int FileCount { get; set; }

    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private static CompilationWithAnalyzers? TestCompilation { get; set; }

    /// <summary>
    /// Prepares Roslyn compilation objects with generated source files for benchmarking, based on the current <c>FileCount</c>.
    /// </summary>
    /// <remarks>
    /// Generates a set of source files, each containing an interface and a class with a Moq setup that intentionally mismatches callback signatures. 
    /// Initializes both baseline and test compilations with these sources and the specified reference assemblies, ready for analyzer benchmarking.
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

public interface IFoo{index}
{{
    int Do(string s);
    int Do(int i, string s, DateTime dt);
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<IFoo{index}>().Setup(x => x.Do(It.IsAny<string>())).Callback((int i) => {{ }});
        _ = ""sample test""; // Add an expression that looks similar but does not match
    }}
}}
"));
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies("Net80WithOldMoq");
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory.CreateAsync<CallbackSignatureShouldMatchMockedMethodAnalyzer>(sources.ToArray(), referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Benchmarks the analyzer by verifying that the number of diagnostics reported matches the number of generated source files.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the number of diagnostics does not equal <c>FileCount</c>.
    /// </exception>
    [Benchmark]
    public async Task Moq1100WithDiagnostics()
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
    public async Task Moq1100Baseline()
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
