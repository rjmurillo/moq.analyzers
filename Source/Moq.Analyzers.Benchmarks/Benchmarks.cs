using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Diagnostics.Tracing;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
public class CSharp_CA1416
{
    [IterationSetup]
    public static void CreateEnvironmentCA1416()
    {
        var sources = new SourceFileCollection();
        for (var i = 0; i < Constants.NumberOfCodeFiles; i++)
        {
            var name = "TypeName" + i;
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
//            name += "Unsupported";
//            sources.Add((name + "Unsupport", @$"
//using System;
//using PlatformCompatDemo.SupportedUnupported;

//class {name}
//{{
//    private B field = new B();
//    public void M1()
//    {{
//        field.M3();
//    }}
//}}
//"));

//            name += "Flow";
//            sources.Add((name, @$"
//using System;
//using PlatformCompatDemo.SupportedUnupported;

//class {name}
//{{
//    private B field = new B();
//    public void M1()
//    {{
//        if (OperatingSystem.IsWindowsVersionAtLeast(10, 2))
//        {{
//            field.M2();
//        }}
//        else
//        {{
//            field.M2();
//        }}
//    }}
//}}
//"));
        }

//        var targetTypesForTest = @"
//using System.Runtime.Versioning;
//namespace PlatformCompatDemo.SupportedUnupported
//{
//    public class B
//    {
//        [SupportedOSPlatform(""Windows10.1.1.1"")]
//        public void M2() { }
//        [UnsupportedOSPlatform(""macOS11.0.1"")]
//        public void M3() {}
//    }
//}";
//        sources.Add((nameof(targetTypesForTest), targetTypesForTest));
        (string, string)[] properties = new[]
        {
            ("build_property.TargetFramework", "net8.0"),
            ("build_property._SupportedPlatformList", "Linux,macOS"),
        };

        CompilationWithAnalyzers = BenchmarkCSharpAnalyzerVerifier<AsShouldBeUsedOnlyForInterfaceAnalyzer>.VerifyAnalyzerAsync(sources);
        BaselineCompilationWithAnalyzers = BenchmarkCSharpAnalyzerVerifier<EmptyDiagnosticAnalyzer>.VerifyAnalyzerAsync(sources);

        //var (compilation, options) = CSharpCompilationCreator.CreateWithOptionsAsync(sources.ToArray(), properties).GetAwaiter().GetResult();
        //BaselineCompilationWithAnalyzers = compilation.WithAnalyzers([new EmptyDiagnosticAnalyzer()], options, CancellationToken.None);
        //CompilationWithAnalyzers = compilation.WithAnalyzers([new AsShouldBeUsedOnlyForInterfaceAnalyzer()], options, CancellationToken.None);
    }

    private static BenchmarkCSharpAnalyzerVerifier<AsShouldBeUsedOnlyForInterfaceAnalyzer>.Test CompilationWithAnalyzers;
    private static BenchmarkCSharpAnalyzerVerifier<EmptyDiagnosticAnalyzer>.Test BaselineCompilationWithAnalyzers;

    //private static CompilationWithAnalyzers BaselineCompilationWithAnalyzers;
    //private static CompilationWithAnalyzers CompilationWithAnalyzers;

    [Benchmark]
    public async Task CA1416_DiagnosticsProduced()
    {
        await CompilationWithAnalyzers.RunAsync(CancellationToken.None);
        //AnalysisResult analysisResult = await CompilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);
        //ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics = analysisResult.GetAllDiagnostics(analysisResult.Analyzers.First());
        //if (analysisResult.Analyzers.Length != 1)
        //{
        //    throw new InvalidOperationException($"Expected a single analyzer but found '{analysisResult.Analyzers.Length}'");
        //}

        //if (analysisResult.CompilationDiagnostics.Count != 0)
        //{
        //    throw new InvalidOperationException($"Expected no compilation diagnostics but found '{analysisResult.CompilationDiagnostics.Count}'");
        //}

        //if (diagnostics.Length != 1 * Constants.NumberOfCodeFiles)
        //{
        //    throw new InvalidOperationException($"Expected '{1 * Constants.NumberOfCodeFiles:N0}' analyzer diagnostics but found '{diagnostics.Length}'");
        //}
    }

    [Benchmark(Baseline = true)]
    public async Task CA1416_Baseline()
    {
        await BaselineCompilationWithAnalyzers.RunAsync(CancellationToken.None);
        //AnalysisResult analysisResult = await BaselineCompilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None);
        //ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics = analysisResult.GetAllDiagnostics(analysisResult.Analyzers.First());
        //if (analysisResult.Analyzers.Length != 1)
        //{
        //    throw new InvalidOperationException($"Expected a single analyzer but found '{analysisResult.Analyzers.Length}'");
        //}

        //if (analysisResult.CompilationDiagnostics.Count != 0)
        //{
        //    throw new InvalidOperationException($"Expected no compilation diagnostics but found '{analysisResult.CompilationDiagnostics.Count}'");
        //}

        //if (diagnostics.Length != 0)
        //{
        //    throw new InvalidOperationException($"Expected no analyzer diagnostics but found '{diagnostics.Length}'");
        //}
    }
}
