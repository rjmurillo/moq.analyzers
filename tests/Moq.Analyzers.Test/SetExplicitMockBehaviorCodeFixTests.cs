using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.SetExplicitMockBehaviorAnalyzer, Moq.CodeFixes.SetExplicitMockBehaviorFixer>;

namespace Moq.Analyzers.Test;

public class SetExplicitMockBehaviorCodeFixTests
{
    private readonly ITestOutputHelper _output;

    public SetExplicitMockBehaviorCodeFixTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> mockConstructors = new object[][]
        {
            [
                """{|Moq1400:new Mock<ISample>()|};""",
                """new Mock<ISample>(MockBehavior.Loose);""",
            ],
            [
                """{|Moq1400:new Mock<ISample>(MockBehavior.Default)|};""",
                """new Mock<ISample>(MockBehavior.Loose);""",
            ],
            [
                """new Mock<ISample>(MockBehavior.Loose);""",
                """new Mock<ISample>(MockBehavior.Loose);""",
            ],
            [
                """new Mock<ISample>(MockBehavior.Strict);""",
                """new Mock<ISample>(MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> mockConstructorsWithExpressions = new object[][]
        {
            [
                """{|Moq1400:new Mock<Calculator>(() => new Calculator())|};""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
            ],
            [
                """{|Moq1400:new Mock<Calculator>(() => new Calculator(), MockBehavior.Default)|};""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
            ],
            [
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
            ],
            [
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Strict);""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        IEnumerable<object[]> fluentBuilders = new object[][]
        {
            [
                """{|Moq1400:Mock.Of<ISample>()|};""",
                """Mock.Of<ISample>(MockBehavior.Loose);""",
            ],
            [
                """{|Moq1400:Mock.Of<ISample>(MockBehavior.Default)|};""",
                """Mock.Of<ISample>(MockBehavior.Loose);""",
            ],
            [
                """Mock.Of<ISample>(MockBehavior.Loose);""",
                """Mock.Of<ISample>(MockBehavior.Loose);""",
            ],
            [
                """Mock.Of<ISample>(MockBehavior.Strict);""",
                """Mock.Of<ISample>(MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        IEnumerable<object[]> mockRepositories = new object[][]
        {
            [
                """{|Moq1400:new MockRepository(MockBehavior.Default)|};""",
                """new MockRepository(MockBehavior.Loose);""",
            ],
            [
                """new MockRepository(MockBehavior.Loose);""",
                """new MockRepository(MockBehavior.Loose);""",
            ],
            [
                """new MockRepository(MockBehavior.Strict);""",
                """new MockRepository(MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return mockConstructors.Union(mockConstructorsWithExpressions).Union(fluentBuilders).Union(mockRepositories);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMocksWithoutExplicitMockBehavior(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        static string Template(string ns, string mock) =>
            $$"""
            {{ns}}

            public interface ISample
            {
                int Calculate(int a, int b);
            }

            public class Calculator
            {
                public int Calculate(int a, int b)
                {
                    return a + b;
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

    // The following tests were removed because the early return paths in RegisterCodeFixesAsync
    // (e.g., when TryGetEditProperties returns false or nodeToFix is null) cannot be triggered
    // via the public analyzer/codefix APIs or test harness. These paths are not testable without
    // breaking encapsulation or using unsupported reflection/mocking of Roslyn internals.
}
