using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Test.Helpers;
using Xunit;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests for the enhanced AnalyzerVerifier and CodeFixVerifier classes to ensure they work with DiagnosticResult objects.
/// These tests validate the API changes rather than specific analyzer behavior.
/// </summary>
public class EnhancedVerifierTests
{
    /// <summary>
    /// Tests that the enhanced AnalyzerVerifier API accepts DiagnosticResult objects without compilation errors.
    /// This test validates that the new overloads work correctly even if no diagnostics are expected.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task AnalyzerVerifier_WithEmptyDiagnosticArray_CompilesAndRuns()
    {
        // Arrange - Simple valid code that should not trigger any diagnostics
        const string source = """
            public class TestClass 
            {
                public void TestMethod()
                {
                    // Simple method with no Moq usage
                }
            }
            """;

        // Create empty diagnostics array to test the API
        var expectedDiagnostics = System.Array.Empty<DiagnosticResult>();

        // Act & Assert - This tests that the enhanced API compiles and can be called
        await AnalyzerVerifier<SetExplicitMockBehaviorAnalyzer>
            .VerifyAnalyzerAsync(source, expectedDiagnostics, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Tests that all the new API overloads compile correctly.
    /// This validates that the method signatures are available and correct.
    /// </summary>
    [Fact]
    public void AllNewApiOverloads_CompileCorrectly()
    {
        // Act & Assert - These should all compile without errors
        // AnalyzerVerifier overloads - check method exists
        Assert.NotNull(typeof(AnalyzerVerifier<SetExplicitMockBehaviorAnalyzer>)
            .GetMethod(
                nameof(AnalyzerVerifier<SetExplicitMockBehaviorAnalyzer>.VerifyAnalyzerAsync),
                new[] { typeof(string), typeof(DiagnosticResult[]), typeof(string) }));

        Assert.NotNull(typeof(AnalyzerVerifier<SetExplicitMockBehaviorAnalyzer>)
            .GetMethod(
                nameof(AnalyzerVerifier<SetExplicitMockBehaviorAnalyzer>.VerifyAnalyzerAsync),
                new[] { typeof(string), typeof(DiagnosticResult), typeof(string) }));

        // CodeFixVerifier overloads
        Assert.NotNull(typeof(CodeFixVerifier<SetExplicitMockBehaviorAnalyzer, Moq.CodeFixes.SetExplicitMockBehaviorFixer>)
            .GetMethod(
                nameof(CodeFixVerifier<SetExplicitMockBehaviorAnalyzer, Moq.CodeFixes.SetExplicitMockBehaviorFixer>.VerifyCodeFixAsync),
                new[] { typeof(string), typeof(string), typeof(DiagnosticResult[]), typeof(string) }));

        Assert.NotNull(typeof(CodeFixVerifier<SetExplicitMockBehaviorAnalyzer, Moq.CodeFixes.SetExplicitMockBehaviorFixer>)
            .GetMethod(
                nameof(CodeFixVerifier<SetExplicitMockBehaviorAnalyzer, Moq.CodeFixes.SetExplicitMockBehaviorFixer>.VerifyCodeFixAsync),
                new[] { typeof(string), typeof(string), typeof(DiagnosticResult), typeof(string) }));
    }
}
