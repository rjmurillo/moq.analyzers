using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoConstructorArgumentsForInterfaceMockAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoConstructorArgumentsForInterfaceMockAnalyzerTests
{
    public static IEnumerable<object[]> InterfaceMockingTestData()
    {
        return new object[][]
        {
            ["""new Mock<IMyService>{|Moq1001:(25, true)|};"""],
            ["""new Mock<IMyService>{|Moq1001:("123")|};"""],
            ["""new Mock<IMyService>{|Moq1001:(MockBehavior.Default, "123")|};"""],
            ["""new Mock<IMyService>{|Moq1001:(MockBehavior.Strict, 25, true)|};"""],
            ["""new Mock<IMyService>{|Moq1001:(MockBehavior.Loose, 25, true)|};"""],
            ["""new Mock<IMyService>();"""],
            ["""new Mock<IMyService>(MockBehavior.Default);"""],
            ["""new Mock<IMyService>(MockBehavior.Strict);"""],
            ["""new Mock<IMyService>(MockBehavior.Loose);"""],
        }.WithNamespaces().WithReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(InterfaceMockingTestData))]
    public async Task ShouldAnalyzeInterfaceConstructors(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal interface IMyService
                {
                    void Do(string s);
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    // TODO: This feels like it should be in every analyzer's tests. Tracked by #75.
    [Fact]
    public async Task ShouldPassIfCustomMockClassIsUsed()
    {
        await Verifier.VerifyAnalyzerAsync(
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
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    // TODO: This feels like it should be in every analyzer's tests. Tracked by #75.
    [Fact]
    public async Task ShouldFailIsRealMoqIsUsedWithInvalidParameters()
    {
        await Verifier.VerifyAnalyzerAsync(
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
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    // TODO: This feels like it should be in every analyzer's tests. Tracked by #75.
    [Fact]
    public async Task ShouldPassIfRealMoqIsUsedWithValidParameters()
    {
        await Verifier.VerifyAnalyzerAsync(
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
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
