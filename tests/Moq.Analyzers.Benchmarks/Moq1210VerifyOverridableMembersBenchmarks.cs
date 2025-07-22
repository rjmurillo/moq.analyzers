using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
[BenchmarkCategory("Moq1210")]
public class Moq1210VerifyOverridableMembersBenchmarks
{
#pragma warning disable ECS0900
    [Params(1, 1_000)]
#pragma warning restore ECS0900
    public int FileCount { get; set; }

    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private static CompilationWithAnalyzers? TestCompilation { get; set; }

    /// <summary>
    /// Generates and compiles a set of source files based on the current <c>FileCount</c> parameter, preparing both baseline and analyzer test compilations for benchmarking.
    /// </summary>
    /// <remarks>
    /// Each generated source file defines a unique class and a test case that triggers the analyzer under test. The method synchronously creates compilations with and without the analyzer attached, storing them for use in benchmark methods.
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

public class SampleClass{index}
{{
    public string Property {{ get; set; }}
    public int Calculate() => 0; // Non-virtual method
    public virtual int CalculateVirtual(int a, int b) => 0; // Virtual method
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<SampleClass{index}>().Verify(x => x.Property);
        _ = ""sample test""; // Add an expression that looks similar but does not match
    }}
}}
"));
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies("Net80WithOldMoq");
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory.CreateAsync<VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer>(sources.ToArray(), referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Benchmarks the analyzer by running it on the test compilation and verifies that the number of diagnostics matches the expected file count.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the number of diagnostics produced does not equal <c>FileCount</c>.
    /// </exception>
    [Benchmark]
    public async Task Moq1210WithDiagnostics()
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
    public async Task Moq1210Baseline()
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
