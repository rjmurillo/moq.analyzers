using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test;

public class NoConstructorArgumentsForInterfaceMockAnalyzerTests : DiagnosticVerifier<NoConstructorArgumentsForInterfaceMockAnalyzer>
{
    [Fact]
    public async Task ShouldFailIfMockedInterfaceHasConstructorParameters()
    {
        await VerifyCSharpDiagnostic(
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
                        var mock1 = new Mock<IMyService>{|Moq1001:(25, true)|};
                        var mock2 = new Mock<IMyService>{|Moq1001:("123")|};
                        var mock3 = new Mock<IMyService>{|Moq1001:(25, true)|};
                        var mock4 = new Mock<IMyService>{|Moq1001:("123")|};
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldFailIfMockedInterfaceHasConstructorParametersAndExplicitMockBehavior()
    {
        await VerifyCSharpDiagnostic(
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
                        var mock1 = new Mock<IMyService>{|Moq1001:(MockBehavior.Default, "123")|};
                        var mock2 = new Mock<IMyService>{|Moq1001:(MockBehavior.Strict, 25, true)|};
                        var mock3 = new Mock<IMyService>{|Moq1001:(MockBehavior.Default, "123")|};
                        var mock4 = new Mock<IMyService>{|Moq1001:(MockBehavior.Loose, 25, true)|};
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassIfMockedInterfaceDoesNotHaveConstructorParameters()
    {
        await VerifyCSharpDiagnostic(
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
            """);
    }

    // TODO: This feels like it should be in every analyzer's tests
    [Fact]
    public async Task ShouldPassIfCustomMockClassIsUsed()
    {
        await VerifyCSharpDiagnostic(
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
                """);
    }

    // TODO: This feels duplicated with other tests
    [Fact]
    public async Task ShouldFailIsRealMoqIsUsedWithInvalidParameters()
    {
        await VerifyCSharpDiagnostic(
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
                        var mock1 = new Moq.Mock<IMyService>{|Moq1001:(1, true)|};
                        var mock2 = new Moq.Mock<IMyService>{|Moq1001:("2")|};
                        var mock3 = new Moq.Mock<IMyService>{|Moq1001:(Moq.MockBehavior.Default, "3")|};
                        var mock4 = new Moq.Mock<IMyService>{|Moq1001:(MockBehavior.Loose, 4, true)|};
                        var mock5 = new Moq.Mock<IMyService>{|Moq1001:(MockBehavior.Default)|};
                        var mock6 = new Moq.Mock<IMyService>{|Moq1001:(MockBehavior.Default)|};
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassIfRealMoqIsUsedWithValidParameters()
    {
        await VerifyCSharpDiagnostic(
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
                """);
    }
}
