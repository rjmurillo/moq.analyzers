using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoConstructorArgumentsForInterfaceMockAnalyzerTests : DiagnosticVerifier
{
    // [Fact]
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

    // [Fact]
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

    // [Fact]
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

    // TODO: This feels like it should be in every analyzer's tests
    // [Fact]
    public Task ShouldPassIfCustomMockClassIsUsed()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace NoConstructorArgumentsForInterfaceMock.TestFakeMoq;

                public enum MockBehavior
                {
                    Default,
                    Strict,
                    Loose,
                }

                internal interface IMyService
                {
                    void Do(string s);
                }

                public class Mock<T>
                    where T : class
                {
                    public Mock() { }

                    public Mock(params object[] ar) { }

                    public Mock(MockBehavior behavior) { }

                    public Mock(MockBehavior behavior, params object[] args) { }
                }

                internal class MyUnitTests
                {
                    private void TestFakeMoq()
                    {
                        var mock1 = new Mock<IMyService>("4");
                        var mock2 = new Mock<IMyService>(5, true);
                        var mock3 = new Mock<IMyService>(MockBehavior.Strict, 6, true);
                        var mock4 = new Mock<IMyService>(Moq.MockBehavior.Default, "5");
                        var mock5 = new Mock<IMyService>(MockBehavior.Strict);
                        var mock6 = new Mock<IMyService>(MockBehavior.Loose);
                    }
                }
                """
            ]));
    }

    // TODO: This feels duplicated with other tests
    // [Fact]
    public Task ShouldFailIsRealMoqIsUsedWithInvalidParameters()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace NoConstructorArgumentsForInterfaceMock.TestRealMoqWithBadParameters;

                public enum MockBehavior
                {
                    Default,
                    Strict,
                    Loose,
                }

                internal interface IMyService
                {
                    void Do(string s);
                }

                public class Mock<T>
                    where T : class
                {
                    public Mock() { }

                    public Mock(params object[] ar) { }

                    public Mock(MockBehavior behavior) { }

                    public Mock(MockBehavior behavior, params object[] args) { }
                }

                internal class MyUnitTests
                {
                    private void TestRealMoqWithBadParameters()
                    {
                        var mock1 = new Moq.Mock<IMyService>(1, true);
                        var mock2 = new Moq.Mock<IMyService>("2");
                        var mock3 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "3");
                        var mock4 = new Moq.Mock<IMyService>(MockBehavior.Loose, 4, true);
                        var mock5 = new Moq.Mock<IMyService>(MockBehavior.Default);
                        var mock6 = new Moq.Mock<IMyService>(MockBehavior.Default);
                    }
                }
                """
            ]));
    }

    // [Fact]
    public Task ShouldPassIfRealMoqIsUsedWithValidParameters()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace NoConstructorArgumentsForInterfaceMock.TestRealMoqWithGoodParameters;

                public enum MockBehavior
                {
                    Default,
                    Strict,
                    Loose,
                }

                internal interface IMyService
                {
                    void Do(string s);
                }

                public class Mock<T>
                    where T : class
                {
                    public Mock() { }

                    public Mock(params object[] ar) { }

                    public Mock(MockBehavior behavior) { }

                    public Mock(MockBehavior behavior, params object[] args) { }
                }

                internal class MyUnitTests
                {
                    private void TestRealMoqWithGoodParameters()
                    {
                        var mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
                        var mock2 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
                    }
                }
                """
            ]));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NoConstructorArgumentsForInterfaceMockAnalyzer();
    }
}
