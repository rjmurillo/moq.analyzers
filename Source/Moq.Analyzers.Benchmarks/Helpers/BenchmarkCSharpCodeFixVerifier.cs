using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

internal static class BenchmarkCSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, EmptyVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new(descriptor);

    public static Test VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        Test test = new() { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }

    public static Test VerifyCodeFixAsync(string source, string fixedSource)
        => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    public static Test VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
        => VerifyCodeFixAsync(source, [expected], fixedSource);

    public static Test VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
    {
        Test test = new()
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test;
        return test;
    }

    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, EmptyVerifier>
    {
        public Test()
        {
            ReferenceAssemblies = ReferenceAssemblyCatalog.Catalog[ReferenceAssemblyCatalog.Net80WithNewMoq];
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck |
                TestBehaviors.SkipSuppressionCheck |
                TestBehaviors.SkipGeneratedSourcesCheck;
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInDocumentCheck |
                CodeFixTestBehaviors.SkipFixAllInProjectCheck |
                CodeFixTestBehaviors.SkipFixAllInSolutionCheck;
        }
    }
}
