using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer, Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodCodeFix>;

namespace Moq.Analyzers.Test;

public class CallbackSignatureShouldMatchMockedMethodCodeFixTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0051:Method is too long", Justification = "Contains test data")]
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns((string s) => { return 0; });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns((string s) => { return 0; });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns((int i, string s, DateTime dt) => { return 0; });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns((int i, string s, DateTime dt) => { return 0; });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns((List<string> l) => { return 0; });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns((List<string> l) => { return 0; });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback({|Moq1100:(int i)|} => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback({|Moq1100:(string s1, string s2)|} => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback({|Moq1100:(string s1, int i1)|} => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback({|Moq1100:(int i)|} => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(() => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(() => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(() => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(() => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback(() => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback(() => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });""",
            ],
        }.WithNamespaces().WithReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldSuggestQuickFixWhenIncorrectCallbacks(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        static string Template(string ns, string mock) =>
            $$"""
            {{ns}}

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(Template(@namespace, original), Template(@namespace, quickFix), referenceAssemblyGroup);
    }
}
