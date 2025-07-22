using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
[BenchmarkCategory("Moq1300")]
public class Moq1300Benchmarks
{
#pragma warning disable ECS0900
    [Params(1, 1_000)]
#pragma warning restore ECS0900
    public int FileCount { get; set; }

    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private static CompilationWithAnalyzers? TestCompilation { get; set; }

    /// <summary>
    /// Prepares Roslyn compilations with a specified number of generated source files for benchmarking the AsShouldBeUsedOnlyForInterfaceAnalyzer.
    /// </summary>
    /// <remarks>
    /// Generates <c>FileCount</c> source files, each containing a sample class and an internal class that exercises the analyzer scenario. 
    /// Initializes both baseline and test compilations for use in benchmark methods.
    /// </remarks>
    [IterationSetup]
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Async setup not supported in BenchmarkDotNet.See https://github.com/dotnet/BenchmarkDotNet/issues/2442.")]
    public void SetupCompilation()
    {
        (string Name, string Content)[] sources = new (string, string)[FileCount];
        for (int index = 0; index < FileCount; index++)
        {
            string name = "TypeName" + index;
            sources[index] = (name, @$"
using System;
using Moq;

public class SampleClass{index}
{{

    public int Calculate() => 0;
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<SampleClass{index}>().As<SampleClass{index}>();
        _ = new SampleClass{index}().Calculate(); // Add an expression that looks similar but does not match
    }}
}}
");
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies("Net80WithOldMoq");
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory.CreateAsync<AsShouldBeUsedOnlyForInterfaceAnalyzer>(sources, referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Benchmarks the analyzer by running diagnostics on the test compilation and verifies that the number of reported diagnostics matches the configured file count.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the number of diagnostics produced does not equal <c>FileCount</c>.
    /// </exception>
    [Benchmark]
    public async Task Moq1300WithDiagnostics()
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
    public async Task Moq1300Baseline()
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
