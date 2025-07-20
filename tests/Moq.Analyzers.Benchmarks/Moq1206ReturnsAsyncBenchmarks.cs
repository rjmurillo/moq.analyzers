using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
public class Moq1206ReturnsAsyncBenchmarks
{
    [Params(1, 10, 100, 1_000)]
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
            string name = $"TypeName{index}";
            sources.Add((name, @$"
using System;
using System.Threading.Tasks;
using Moq;

public class AsyncClient{index}
{{
    public virtual Task TaskAsync() => Task.CompletedTask;
    public virtual Task<string> GenericTaskAsync() => Task.FromResult(string.Empty);
    public virtual ValueTask ValueTaskAsync() => ValueTask.CompletedTask;
    public virtual ValueTask<string> GenericValueTaskAsync() => ValueTask.FromResult(string.Empty);
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<AsyncClient{index}>().Setup(c => c.GenericTaskAsync()).Returns(async () => ""test"");
        _ = ""sample test""; // Add an expression that looks similar but does not match
    }}
}}
"));
        }

        Microsoft.CodeAnalysis.Testing.ReferenceAssemblies referenceAssemblies = CompilationCreator.GetReferenceAssemblies(MoqKey);
        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory
            .CreateAsync<ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer>(sources.ToArray(), referenceAssemblies)
            .GetAwaiter()
            .GetResult();
    }

    [Benchmark]
    public async Task Moq1206WithDiagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics =
            (await TestCompilation!
            .GetAnalysisResultAsync(CancellationToken.None)
            .ConfigureAwait(false))
            .AssertValidAnalysisResult()
            .GetAllDiagnostics();

        // All versions should flag the Returns(async () => ...) pattern
        if (diagnostics.Length != FileCount)
        {
            throw new InvalidOperationException($"Expected '{FileCount:N0}' analyzer diagnostics but found '{diagnostics.Length}'");
        }
    }

    [Benchmark]
    public async Task Moq1206WithoutDiagnostics()
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
