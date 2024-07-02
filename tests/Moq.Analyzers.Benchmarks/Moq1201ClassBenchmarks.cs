﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
public class Moq1201InterfaceBenchmarks
{
    private static CompilationWithAnalyzers? BaselineCompilation { get; set; }
    private static CompilationWithAnalyzers? ControlCompilation { get; set; }
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

public interface ISample{index}
{{
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<ISample{index}>(42);
        Mock.Of<ISample{index}>(42);
    }}
}}
"));
        }

        (BaselineCompilation, ControlCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory
                .CreateAsync<NoConstructorArgumentsForInterfaceMockAnalyzer, ConstructorArgumentsShouldMatchAnalyzer>(sources.ToArray())
                .GetAwaiter()
                .GetResult();
    }

    [Benchmark]
    public async Task<ImmutableArray<Diagnostic>> Moq1201ControlWithDiagnostics()
    {
        ImmutableArray<Diagnostic> diagnostics =
            (await ControlCompilation!
                .GetAnalysisResultAsync(CancellationToken.None)
                .ConfigureAwait(false))
            .AssertValidAnalysisResult()
            .GetAllDiagnostics();

        if (diagnostics.Length != Constants.NumberOfCodeFiles)
        {
            throw new InvalidOperationException($"Expected '{Constants.NumberOfCodeFiles:N0}' analyzer diagnostics but found '{diagnostics.Length}'");
        }

        return diagnostics;
    }

    [Benchmark]
    public async Task<ImmutableArray<Diagnostic>> Moq1201TreatmentWithDiagnostics()
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

        return diagnostics;
    }

    [Benchmark(Baseline = true)]
    public async Task Moq1201Baseline()
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

[InProcess]
[MemoryDiagnoser]
public class Moq1201ClassBenchmarks
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

public class SampleClass{index}
{{
    public SampleClass{index}(int value)
    {{
    }}
    public int Calculate() => 0;
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<SampleClass{index}>();
        _ = new SampleClass{index}(42).Calculate(); // Add an expression that looks similar but does not match
    }}
}}
"));
        }

        (BaselineCompilation, TestCompilation) =
            BenchmarkCSharpCompilationFactory
                .CreateAsync<ConstructorArgumentsShouldMatchAnalyzer>(sources.ToArray())
                .GetAwaiter()
                .GetResult();
    }

    [Benchmark]
    public async Task Moq1201WithDiagnostics()
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
    public async Task Moq1201Baseline()
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
