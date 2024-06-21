using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
public class CSharp_CA1416
{
    private static CompilationWithAnalyzers? BaselineCompilationWithAnalyzers { get; set; }

    private static CompilationWithAnalyzers? CompilationWithAnalyzers { get; set; }

    [IterationSetup]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Async setup not supported in BenchmarkDotNet. See https://github.com/dotnet/BenchmarkDotNet/issues/2442.")]
    public static void CreateEnvironmentCA1416()
    {
        List<(string Name, string Content)> sources = [];
        for (int i = 0; i < Constants.NumberOfCodeFiles; i++)
        {
            string name = "TypeName" + i;
            sources.Add((name, @$"
using System;
using Moq;

public class SampleClass{i}
{{

    public int Calculate() => 0;
}}

internal class {name}
{{
    private void Test()
    {{
        new Mock<SampleClass{i}>().As<SampleClass{i}>();
    }}
}}
"));
        }

        Compilation? compilation = CSharpCompilationCreator.CreateAsync(sources.ToArray()).GetAwaiter().GetResult();
        BaselineCompilationWithAnalyzers = compilation?.WithAnalyzers([new EmptyDiagnosticAnalyzer()], options: null, CancellationToken.None);
        CompilationWithAnalyzers = compilation?.WithAnalyzers([new AsShouldBeUsedOnlyForInterfaceAnalyzer()], options: null, CancellationToken.None);
    }

    [Benchmark]
    public async Task CA1416_DiagnosticsProduced()
    {
        AnalysisResult analysisResult = await CompilationWithAnalyzers!.GetAnalysisResultAsync(CancellationToken.None).ConfigureAwait(false);
        ImmutableArray<Diagnostic> diagnostics = analysisResult.GetAllDiagnostics(analysisResult.Analyzers.First());
        if (analysisResult.Analyzers.Length != 1)
        {
            throw new InvalidOperationException($"Expected a single analyzer but found '{analysisResult.Analyzers.Length}'");
        }

        if (analysisResult.CompilationDiagnostics.Count != 0)
        {
            throw new InvalidOperationException($"Expected no compilation diagnostics but found '{analysisResult.CompilationDiagnostics.Count}'");
        }

        if (diagnostics.Length != 1 * Constants.NumberOfCodeFiles)
        {
            throw new InvalidOperationException($"Expected '{1 * Constants.NumberOfCodeFiles:N0}' analyzer diagnostics but found '{diagnostics.Length}'");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task CA1416_Baseline()
    {
        AnalysisResult analysisResult = await BaselineCompilationWithAnalyzers!.GetAnalysisResultAsync(CancellationToken.None).ConfigureAwait(false);
        ImmutableArray<Diagnostic> diagnostics = analysisResult.GetAllDiagnostics(analysisResult.Analyzers.First());
        if (analysisResult.Analyzers.Length != 1)
        {
            throw new InvalidOperationException($"Expected a single analyzer but found '{analysisResult.Analyzers.Length}'");
        }

        if (analysisResult.CompilationDiagnostics.Count != 0)
        {
            throw new InvalidOperationException($"Expected no compilation diagnostics but found '{analysisResult.CompilationDiagnostics.Count}'");
        }

        if (diagnostics.Length != 0)
        {
            throw new InvalidOperationException($"Expected no analyzer diagnostics but found '{diagnostics.Length}'");
        }
    }
}
