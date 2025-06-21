using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;
using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer, Moq.CodeFixes.CallbackSignatureShouldMatchMockedMethodFixer>;

namespace Moq.Analyzers.Test;

#pragma warning disable SA1204 // Static members should appear before non-static members

public class CallbackSignatureShouldMatchMockedMethodCodeFixTests
{
    private readonly ITestOutputHelper _output;

    public CallbackSignatureShouldMatchMockedMethodCodeFixTests(ITestOutputHelper output)
    {
        _output = output;
    }

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
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(({|Moq1100:int i|}) => { });""",
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
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback(({|Moq1100:int i|}) => { });""",
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
            [ // Repros for https://github.com/rjmurillo/moq.analyzers/issues/172
                """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<object?>())).Returns((object? bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<object?>())).Returns((object? bar) => true);""",
            ],
            [
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((long bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((long bar) => true);""",
            ],
            [
                """new Mock<IFoo>().Setup(m => m.Do((long)42)).Returns((long bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do((long)42)).Returns((long bar) => true);""",
            ],
            [
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((object? bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((object? bar) => true);""",
            ],
            [ // This was also reported as part of 172, but is a different error
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((int bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((int bar) => true);""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
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

                bool Do(object? bar);

                bool Do(long bar);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """;

        string o = Template(@namespace, original);
        string f = Template(@namespace, quickFix);

        _output.WriteLine("Original:");
        _output.WriteLine(o);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Fixed:");
        _output.WriteLine(f);

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> ConversionTestData()
    {
        return new object[][]
            {
                [ // This should be allowed because of the implicit conversion from int to CustomType
                    """new Mock<IFoo>().Setup(x => x.Do(42)).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do(42)).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of identity
                    """new Mock<IFoo>().Setup(x => x.Do(new CustomType(42))).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do(new CustomType(42))).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of the explicit conversion from string to CustomType
                    """new Mock<IFoo>().Setup(x => x.Do((CustomType)"42")).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do((CustomType)"42")).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of numeric conversion (explicit)
                    """new Mock<IFoo>().Setup(x => x.Do((int)42L)).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do((int)42L)).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of numeric conversion (explicit)
                    """new Mock<IFoo>().Setup(x => x.Do((int)42.0)).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do((int)42.0)).Returns((CustomType i) => true);""",
                ],
                [
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<int>())).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<int>())).Returns((CustomType i) => true);""",
                ],
                [
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<CustomType>())).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<CustomType>())).Returns((CustomType i) => true);""",
                ],
            }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ConversionTestData))]
    public async Task ConversionTests(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        static string Template(string ns, string mock) =>
            $$"""
            {{ns}}

            internal interface IFoo
            {
                bool Do(CustomType custom);
            }

            public class CustomType
            {
                public int Value { get; }

                public CustomType(int value)
                {
                    Value = value;
                }

                // User-defined conversions
                public static implicit operator CustomType(int value)
                {
                    return new CustomType(value);
                }

                public static explicit operator CustomType(string str)
                {
                    return new CustomType(int.Parse(str));
                }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """;

        string o = Template(@namespace, original);
        string f = Template(@namespace, quickFix);

        _output.WriteLine("Original:");
        _output.WriteLine(o);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Fixed:");
        _output.WriteLine(f);

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await AnalyzerVerifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
