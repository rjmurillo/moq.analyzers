using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to validate the suppression behavior of ConstructorArgumentsShouldMatchAnalyzer.
/// These tests confirm that Roslyn handles diagnostic suppression correctly without
/// needing custom suppression checks in the analyzer.
/// </summary>
public class ConstructorArgumentsShouldMatchAnalyzerSuppressionTests
{
    [Fact]
    public async Task ShouldProduceDiagnosticsWhenNotSuppressed()
    {
        // This test validates that diagnostics are still produced when not suppressed
        // to ensure our suppression tests are meaningful
        
        var source = """
            namespace Test
            {
                internal class Foo
                {
                    public Foo(int x) { }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<Foo>{|Moq1002:()|};
                    }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, "net6.0");
    }

    [Fact]
    public async Task ShouldNotProduceDiagnosticsWhenSuppressedViaPragma()
    {
        // This test validates pragma warning disable suppression
        
        var source = """
            namespace Test
            {
                internal class Foo
                {
                    public Foo(int x) { }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
#pragma warning disable Moq1002
                        var mock = new Mock<Foo>(); // Should be suppressed
#pragma warning restore Moq1002
                    }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, "net6.0");
    }

    [Fact]
    public async Task ShouldProduceDiagnosticsForInterfaceWithConstructorArgs()
    {
        // Test for interface diagnostic that should be produced when not suppressed
        
        var source = """
            namespace Test
            {
                internal interface IFoo
                {
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<IFoo>{|Moq1001:(42)|};
                    }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, "net6.0");
    }

    [Fact]
    public async Task ShouldNotProduceDiagnosticsForInterfaceWhenSuppressed()
    {
        // Test interface diagnostic suppression via pragma
        
        var source = """
            namespace Test
            {
                internal interface IFoo
                {
                }

                internal class UnitTest
                {
                    private void Test()
                    {
#pragma warning disable Moq1001
                        var mock = new Mock<IFoo>(42); // Should be suppressed
#pragma warning restore Moq1001
                    }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, "net6.0");
    }

    [Fact]
    public async Task ShouldSuppressBothDiagnosticsWhenBothAreDisabled()
    {
        // Test when both diagnostics are suppressed to validate the behavior
        // This confirms Roslyn handles multiple suppressions correctly
        
        var source = """
            namespace Test
            {
                internal interface IFoo
                {
                }

                internal class Foo
                {
                    public Foo(int x) { }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
#pragma warning disable Moq1001, Moq1002
                        var mock1 = new Mock<IFoo>(42); // Should be suppressed
                        var mock2 = new Mock<Foo>();    // Should be suppressed
#pragma warning restore Moq1001, Moq1002
                    }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, "net6.0");
    }
}