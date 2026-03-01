using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.ReturnsDelegateShouldReturnTaskAnalyzer, Moq.CodeFixes.ReturnsDelegateShouldReturnTaskFixer>;

namespace Moq.Analyzers.Test;

public class ReturnsDelegateShouldReturnTaskFixerTests
{
    private readonly ITestOutputHelper _output;

    public ReturnsDelegateShouldReturnTaskFixerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Task<int> with parameterless lambda returning int
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(() => 42)|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(() => 42);""",
            ],

            // Task<string> with parameterless lambda returning string
            [
                """new Mock<AsyncService>().Setup(s => s.GetNameAsync()).{|Moq1208:Returns(() => "hello")|};""",
                """new Mock<AsyncService>().Setup(s => s.GetNameAsync()).ReturnsAsync(() => "hello");""",
            ],

            // ValueTask<int> with parameterless lambda returning int
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueTaskAsync()).{|Moq1208:Returns(() => 42)|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueTaskAsync()).ReturnsAsync(() => 42);""",
            ],

            // Delegate with parameter
            [
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).{|Moq1208:Returns((string x) => x.Length)|};""",
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).ReturnsAsync((string x) => x.Length);""",
            ],

            // Parenthesized Setup expression
            [
                """(new Mock<AsyncService>().Setup(s => s.GetValueAsync())).{|Moq1208:Returns(() => 42)|};""",
                """(new Mock<AsyncService>().Setup(s => s.GetValueAsync())).ReturnsAsync(() => 42);""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldReplaceReturnsWithReturnsAsync(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        static string Template(string ns, string mock) =>
            $$"""
            {{ns}}

            public class AsyncService
            {
                public virtual Task<int> GetValueAsync() => Task.FromResult(0);
                public virtual Task<string> GetNameAsync() => Task.FromResult(string.Empty);
                public virtual ValueTask<int> GetValueTaskAsync() => new ValueTask<int>(0);
                public virtual Task<int> ProcessAsync(string input) => Task.FromResult(input.Length);
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
}
