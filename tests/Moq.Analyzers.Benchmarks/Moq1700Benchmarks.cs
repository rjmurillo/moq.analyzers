using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
public class Moq1700Benchmarks
{
    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }

    private static CompilationWithAnalyzers? TestCompilation { get; set; }

    [IterationSetup]
    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Async setup not supported in BenchmarkDotNet.See https://github.com/dotnet/BenchmarkDotNet/issues/2442.")]
    public static void SetupCompilation()
    {
        List<(string Name, string Content)> sources = [];
        for (int index = 0; index < Constants.NumberOfCodeFiles; index++)
        {
            string name = "TypeName" + index;
            sources.Add((name, @$"
using System;
using Moq;

public interface ISampleInterface{index}
{{
    string GetValue();
}}

internal class {name}
{{
    private void Test()
    {{
        var mock = new Mock<ISampleInterface{index}>();
        mock.Setup(x => x.GetValue()).Returns(""first"");
        mock.SetupSequence(x => x.GetValue()).Returns(""second""); // This should trigger diagnostic
        _ = mock.Object.GetValue();
    }}
}}
"));
        }

        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory
            .CreateAsync<SequenceSetupAfterStandardSetupAnalyzer>(sources.ToArray())
            .GetAwaiter()
            .GetResult();
    }

    [Benchmark]
    public async Task Moq1700WithDiagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics =
            (await TestCompilation!
            .GetAnalysisResultAsync(CancellationToken.None)
            .ConfigureAwait(false))
            .AssertValidAnalysisResult()
            .GetAllDiagnostics();

        if (diagnostics.Length != Constants.NumberOfCodeFiles)
        {
            throw new InvalidOperationException($"Expected '{Constants.NumberOfCodeFiles:N0}' analyzer diagnostics but found '{diagnostics.Length}'");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task Moq1700Baseline()
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
