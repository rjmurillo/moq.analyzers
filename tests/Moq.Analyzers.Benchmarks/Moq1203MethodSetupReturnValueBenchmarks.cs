using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
[BenchmarkCategory("Moq1203")]
public class Moq1203MethodSetupReturnValueBenchmarks
{
#pragma warning disable ECS0900
    [Params(1, 10, 100, 1_000)]
#pragma warning restore ECS0900
    public int FileCount { get; set; }

    [Params("Net80WithOldMoq", "Net80WithNewMoq")]
    public string MoqKey { get; set; } = "Net80WithOldMoq";

    private CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private CompilationWithAnalyzers? TestCompilation { get; set; }

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

public interface IService{index}
{{
    string GetValue();
    int Calculate(int x, int y);
    void DoVoidWork();
}}

internal class {name}
{{
    private void Test()
    {{
        var mock = new Mock<IService{index}>();
        mock.Setup(x => x.GetValue()); // Should trigger diagnostic
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>())); // Should trigger diagnostic
        mock.Setup(x => x.DoVoidWork()); // Should not trigger diagnostic (void method)
        _ = ""sample test""; // Add an expression that looks similar but does not match
    }}
}}
"));
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies(MoqKey);
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory
            .CreateAsync<MethodSetupShouldSpecifyReturnValueAnalyzer>(sources.ToArray(), referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    [Benchmark]
    public async Task Moq1203WithDiagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics =
            (await TestCompilation!
            .GetAnalysisResultAsync(CancellationToken.None)
            .ConfigureAwait(false))
            .AssertValidAnalysisResult()
            .GetAllDiagnostics();

        // Each file should produce 2 diagnostics (GetValue and Calculate methods, but not DoVoidWork)
        int expectedDiagnostics = FileCount * 2;
        if (diagnostics.Length != expectedDiagnostics)
        {
            throw new InvalidOperationException($"Expected '{expectedDiagnostics:N0}' analyzer diagnostics but found '{diagnostics.Length}'");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Moq1203Baseline()
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
