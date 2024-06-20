using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

internal static class BenchmarkCSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId = null)
        => CSharpCodeFixVerifier<TAnalyzer, EmptyCodeFixProvider, EmptyVerifier>.Diagnostic(diagnosticId);
    //CSharpAnalyzerVerifier<TAnalyzer, EmptyVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) => new(descriptor);

    //public static Test VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    //{
    //    Test test = new() { TestCode = source };
    //    test.ExpectedDiagnostics.AddRange(expected);
    //    return test;
    //}

    public static Test VerifyAnalyzerAsync(SourceFileCollection sources, params DiagnosticResult[] expected)
    {
        Test test = new();
        test.TestState.Sources.AddRange(sources);
        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }

    // Code fix tests support both analyzer and code fix testing. This test class is derived from the code fix test
    // to avoid the need to maintain duplicate copies of the customization work.
    public class Test : BenchmarkCSharpCodeFixVerifier<TAnalyzer, EmptyCodeFixProvider>.Test
    {
    }
}
