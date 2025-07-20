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
    [Params(1, 10, 100, 1_000)]
    public int FileCount { get; set; }

    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private static CompilationWithAnalyzers? TestCompilation { get; set; }

    [IterationSetup]
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Async setup not supported in BenchmarkDotNet.See https://github.com/dotnet/BenchmarkDotNet/issues/2442.")]
    public void SetupCompilation()
    {
        (string Name, string Content)[] sources = new (string, string)[FileCount];
        for (int index = 0; index < FileCount; index++)
        {
            string name = string.Format(System.Globalization.CultureInfo.InvariantCulture, "TypeName{0}", index);
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
