using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
[BenchmarkCategory("Moq1500")]
public class Moq1500MockRepositoryBenchmarks
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
    /// Generates the specified number of source files, each containing a unique interface and class that omits a <c>repository.Verify()</c> call, then creates baseline and test compilations with the relevant analyzers applied.
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

public interface ISample{index}
{{
    int Calculate(int a, int b);
    void DoSomething();
}}

internal class {name}
{{
    private void TestMethod()
    {{
        var repository = new MockRepository(MockBehavior.Strict); // This should trigger Moq1500
        var mock = repository.Create<ISample{index}>();
        
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(42);
        mock.Setup(x => x.DoSomething());
        
        // Test logic would go here
        
        // Missing repository.Verify() call
    }}
}}
");
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies("Net80WithOldMoq");
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory.CreateAsync<MockRepositoryVerifyAnalyzer>(sources, referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Benchmarks the analyzer by verifying that the expected number of diagnostics are reported for the generated test sources.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the number of diagnostics found does not match the expected <c>FileCount</c>.
    /// </exception>
    [Benchmark]
    public async Task Moq1500WithDiagnostics()
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
    public async Task Moq1500Baseline()
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
