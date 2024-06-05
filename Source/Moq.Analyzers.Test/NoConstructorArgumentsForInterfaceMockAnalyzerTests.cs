using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoConstructorArgumentsForInterfaceMockAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailIfMockedInterfaceHasConstructorParameters()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                using Moq;

                namespace NoConstructorArgumentsForInterfaceMock.TestBad;

                internal interface IMyService
                {
                    void Do(string s);
                }

                internal class MyUnitTests
                {
                    private void TestBad()
                    {
                        var mock1 = new Mock<IMyService>(25, true);
                        var mock2 = new Mock<IMyService>("123");
                        var mock3 = new Mock<IMyService>(25, true);
                        var mock4 = new Mock<IMyService>("123");
                    }
                }
                """
            ]));
    }

    [Fact]
    public Task ShouldFailIfMockedInterfaceHasConstructorParametersAndExplicitMockBehavior()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                using Moq;

                namespace NoConstructorArgumentsForInterfaceMock.TestBadWithMockBehavior;

                internal interface IMyService
                {
                    void Do(string s);
                }

                internal class MyUnitTests
                {
                    private void TestBadWithMockBehavior()
                    {
                        var mock1 = new Mock<IMyService>(MockBehavior.Default, "123");
                        var mock2 = new Mock<IMyService>(MockBehavior.Strict, 25, true);
                        var mock3 = new Mock<IMyService>(MockBehavior.Default, "123");
                        var mock4 = new Mock<IMyService>(MockBehavior.Loose, 25, true);
                    }
                }
                """
            ]));
    }

    [Fact]
    public Task ShouldPassIfMockedInterfaceDoesNotHaveConstructorParameters()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
            """
            using Moq;

            namespace NoConstructorArgumentsForInterfaceMock.TestGood;

            internal interface IMyService
            {
                void Do(string s);
            }

            internal class MyUnitTests
            {
                private void TestGood()
                {
                    var mock1 = new Mock<IMyService>();
                    var mock2 = new Mock<IMyService>(MockBehavior.Default);
                    var mock3 = new Mock<IMyService>(MockBehavior.Strict);
                    var mock4 = new Mock<IMyService>(MockBehavior.Loose);
                }
            }
            """
            ]));
    }

    [Fact]
    public Task ShouldPassIfCustomMockClassIsUsed()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock_2.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NoConstructorArgumentsForInterfaceMockAnalyzer();
    }
}
