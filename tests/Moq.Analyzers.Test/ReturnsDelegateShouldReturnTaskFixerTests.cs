using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.ReturnsDelegateShouldReturnTaskAnalyzer, Moq.CodeFixes.ReturnsDelegateShouldReturnTaskFixer>;

namespace Moq.Analyzers.Test;

public class ReturnsDelegateShouldReturnTaskFixerTests(ITestOutputHelper output)
{
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

            // Block-bodied lambda returning wrong type
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(() => { return 42; })|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(() => { return 42; });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Anonymous methods and method groups with type mismatches produce compiler errors
    /// (CS0029/CS1662), unlike lambdas. We suppress compiler diagnostics to isolate the fixer.
    /// </summary>
    /// <returns>Test data with compiler diagnostic suppression for anonymous delegate and method group cases.</returns>
    public static IEnumerable<object[]> AnonymousDelegateAndMethodGroupTestData()
    {
        return new object[][]
        {
            // Anonymous method returning int on Task<int> method
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(delegate { return 42; })|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(delegate { return 42; });""",
            ],

            // Anonymous method with parameter returning wrong type on Task<int> method
            [
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).{|Moq1208:Returns(delegate (string x) { return x.Length; })|};""",
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).ReturnsAsync(delegate (string x) { return x.Length; });""",
            ],

            // Method group returning int on Task<int> method
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(GetInt)|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(GetInt);""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldReplaceReturnsWithReturnsAsync(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, original, quickFix);
    }

    [Theory]
    [MemberData(nameof(AnonymousDelegateAndMethodGroupTestData))]
    public async Task ShouldReplaceReturnsWithReturnsAsyncForAnonymousDelegateAndMethodGroup(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, original, quickFix, CompilerDiagnostics.None);
    }

    private static string Template(string ns, string mock) =>
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
            private static int GetInt() => 42;

            private void Test()
            {
                {{mock}}
            }
        }
        """;

    private async Task VerifyAsync(string referenceAssemblyGroup, string @namespace, string original, string quickFix, CompilerDiagnostics? compilerDiagnostics = null)
    {
        string o = Template(@namespace, original);
        string f = Template(@namespace, quickFix);

        output.WriteLine("Original:");
        output.WriteLine(o);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(f);

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup, compilerDiagnostics).ConfigureAwait(false);
    }
}
