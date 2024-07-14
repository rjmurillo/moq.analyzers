using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1601 // Partial elements should be documented

public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> CustomMockClassIsUsedData()
    {
        return new object[][]
        {
            ["""var mock1 = new Mock<IMyService>("4");"""],
            ["""var mock2 = new Mock<IMyService>(5, true);"""],
            ["""var mock3 = new Mock<IMyService>(MockBehavior.Strict, 6, true);"""],
            ["""var mock4 = new Mock<IMyService>(Moq.MockBehavior.Default, "5");"""],
            ["""var mock5 = new Mock<IMyService>(MockBehavior.Strict);"""],
            ["""var mock6 = new Mock<IMyService>(MockBehavior.Loose);"""],
        };
    }

    // TODO: This feels like it should be in every analyzer's tests. Tracked by #75.
    [Theory]
    [MemberData(nameof(CustomMockClassIsUsedData))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
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
                        {{mock}}
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    public static IEnumerable<object[]> RealMoqIsUsedWithInvalidParameters()
    {
        return new object[][]
        {
            ["""var mock1 = new Moq.Mock<IMyService>{|Moq1001:(1, true)|};"""],
            ["""var mock2 = new Moq.Mock<IMyService>{|Moq1001:("2")|};"""],
            ["""var mock3 = new Moq.Mock<IMyService>{|Moq1001:(Moq.MockBehavior.Default, "3")|};"""],
            ["""var mock4 = new Moq.Mock<IMyService>{|Moq1001:(MockBehavior.Loose, 4, true)|};"""],
        };
    }

    // TODO: This feels like it should be in every analyzer's tests. Tracked by #75.
    [Theory]
    [MemberData(nameof(RealMoqIsUsedWithInvalidParameters))]
    public async Task ShouldFailIsRealMoqIsUsedWithInvalidParameters(string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
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
                        {{mock}}
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
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1204 // Static elements should appear before instance elements
#pragma warning restore SA1601 // Partial elements should be documented
