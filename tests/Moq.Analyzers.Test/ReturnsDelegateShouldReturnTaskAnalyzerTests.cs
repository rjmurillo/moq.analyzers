using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ReturnsDelegateShouldReturnTaskAnalyzer>;

namespace Moq.Analyzers.Test;

public class ReturnsDelegateShouldReturnTaskAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> ValidTestData()
    {
        IEnumerable<object[]> data = new object[][]
        {
            // Delegate returns Task<T> (correct)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(() => Task.FromResult(42));"""],

            // Uses ReturnsAsync (correct, different overload)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).ReturnsAsync(42);"""],

            // Direct value, not a delegate
            ["""new Mock<AsyncService>().Setup(c => c.GetSync()).Returns(42);"""],

            // Non-async method with sync delegate (no mismatch)
            ["""new Mock<AsyncService>().Setup(c => c.GetSync()).Returns(() => 42);"""],

            // Async lambda (Moq1206's domain, not ours)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(async () => 42);"""],

            // Setup without Returns call
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync());"""],

            // Delegate returns ValueTask<T> (correct)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueTaskAsync()).Returns(() => ValueTask.FromResult(42));"""],

            // Chained Callback with correct Task return
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Callback(() => { }).Returns(() => Task.FromResult(42));"""],

            // Anonymous method returning Task.FromResult (correct)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(delegate { return Task.FromResult(42); });"""],

            // Async anonymous method (Moq1206's domain, not ours)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(async delegate { return 42; });"""],

            // Method group returning Task<int> (correct)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(GetIntAsync);"""],

            // Anonymous method on sync method (no mismatch)
            ["""new Mock<AsyncService>().Setup(c => c.GetSync()).Returns(delegate { return 42; });"""],

            // Method group on sync method (no mismatch)
            ["""new Mock<AsyncService>().Setup(c => c.GetSync()).Returns(GetInt);"""],

            // Direct value on async method (not a delegate, different Returns overload)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(Task.FromResult(42));"""],

            // Non-generic Task method with sync delegate (no mismatch, Task has no inner type)
            ["""new Mock<AsyncService>().Setup(c => c.DoAsync()).Returns(() => Task.CompletedTask);"""],

            // Parenthesized Setup with ReturnsAsync (valid)
            ["""(new Mock<AsyncService>().Setup(c => c.GetValueAsync())).ReturnsAsync(42);"""],

            // Block-bodied lambda returning Task (correct)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(() => { return Task.FromResult(42); });"""],

            // Generic non-Task return type (IList<int> is generic but not Task/ValueTask)
            ["""new Mock<AsyncService>().Setup(c => c.GetItems()).Returns(() => new List<int>());"""],

            // Property setup (resolves to IPropertySymbol, not IMethodSymbol)
            ["""new Mock<AsyncService>().Setup(c => c.Value).Returns(() => Task.FromResult(42));"""],

            // Split setup/returns (FindSetupInvocation can't walk past variable reference)
            [
                """
                var setup = new Mock<AsyncService>().Setup(c => c.GetValueAsync());
                setup.Returns(() => Task.FromResult(42));
                """,
            ],

            // Expression variable setup (Setup argument is not a lambda, so mocked member can't be extracted)
            [
                """
                System.Linq.Expressions.Expression<Func<AsyncService, Task<int>>> expr = c => c.GetValueAsync();
                new Mock<AsyncService>().Setup(expr).Returns(() => Task.FromResult(42));
                """,
            ],
        };

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidTestData()
    {
        IEnumerable<object[]> data = new object[][]
        {
            // Sync delegate returning int on Task<int> method
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).{|Moq1208:Returns(() => 42)|};"""],

            // Sync delegate returning string on Task<string> method
            ["""new Mock<AsyncService>().Setup(c => c.GetNameAsync()).{|Moq1208:Returns(() => "hello")|};"""],

            // Sync delegate returning int on ValueTask<int> method
            ["""new Mock<AsyncService>().Setup(c => c.GetValueTaskAsync()).{|Moq1208:Returns(() => 42)|};"""],

            // Parenthesized Setup with sync delegate mismatch
            ["""(new Mock<AsyncService>().Setup(c => c.GetValueAsync())).{|Moq1208:Returns(() => 42)|};"""],

            // Chained Callback then Returns with sync delegate mismatch
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Callback(() => { }).{|Moq1208:Returns(() => 42)|};"""],

            // Block-bodied lambda returning wrong type on Task<int> method
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).{|Moq1208:Returns(() => { return 42; })|};"""],

            // Sync delegate with parameter returning wrong type on Task<int> method
            ["""new Mock<AsyncService>().Setup(c => c.ProcessAsync(It.IsAny<string>())).{|Moq1208:Returns((string x) => x.Length)|};"""],
        };

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Anonymous methods and method groups with type mismatches produce compiler errors
    /// (CS0029/CS1662), unlike lambdas. We suppress compiler diagnostics to isolate the analyzer.
    /// </summary>
    /// <returns>Test data with compiler diagnostic suppression for anonymous delegate and method group cases.</returns>
    public static IEnumerable<object[]> InvalidAnonymousDelegateAndMethodGroupTestData()
    {
        IEnumerable<object[]> data = new object[][]
        {
            // Anonymous method returning int on Task<int> method
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).{|Moq1208:Returns(delegate { return 42; })|};"""],

            // Anonymous method returning int on ValueTask<int> method
            ["""new Mock<AsyncService>().Setup(c => c.GetValueTaskAsync()).{|Moq1208:Returns(delegate { return 42; })|};"""],

            // Anonymous method with parameter returning wrong type on Task<int> method
            ["""new Mock<AsyncService>().Setup(c => c.ProcessAsync(It.IsAny<string>())).{|Moq1208:Returns(delegate (string x) { return x.Length; })|};"""],

            // Method group returning int on Task<int> method
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).{|Moq1208:Returns(GetInt)|};"""],

            // Method group returning string on Task<string> method
            ["""new Mock<AsyncService>().Setup(c => c.GetNameAsync()).{|Moq1208:Returns(GetString)|};"""],
        };

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Valid patterns that produce compiler errors (CS0029/CS1662) but should not trigger the analyzer.
    /// We suppress compiler diagnostics to isolate the analyzer.
    /// </summary>
    /// <returns>Test data with compiler diagnostic suppression.</returns>
    public static IEnumerable<object[]> ValidWithCompilerSuppression()
    {
        IEnumerable<object[]> data = new object[][]
        {
            // Void anonymous delegate on async method (delegate return type is null, no mismatch to report)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(delegate { });"""],
        };

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ValidTestData))]
    public async Task ShouldNotTriggerOnValidPatterns(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(ValidWithCompilerSuppression))]
    public async Task ShouldNotTriggerOnValidPatternsWithCompilerSuppression(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, mock, CompilerDiagnostics.None);
    }

    [Theory]
    [MemberData(nameof(InvalidTestData))]
    public async Task ShouldTriggerOnSyncDelegateMismatch(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(InvalidAnonymousDelegateAndMethodGroupTestData))]
    public async Task ShouldFlagAnonymousDelegateAndMethodGroupMismatch(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, mock, CompilerDiagnostics.None);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    private async Task VerifyAsync(string referenceAssemblyGroup, string @namespace, string mock, CompilerDiagnostics? compilerDiagnostics = null)
    {
        string source =
            $$"""
              {{@namespace}}

              public class AsyncService
              {
                  public virtual Task<int> GetValueAsync() => Task.FromResult(0);
                  public virtual Task<string> GetNameAsync() => Task.FromResult(string.Empty);
                  public virtual ValueTask<int> GetValueTaskAsync() => ValueTask.FromResult(0);
                  public virtual Task DoAsync() => Task.CompletedTask;
                  public virtual int GetSync() => 0;
                  public virtual Task<int> ProcessAsync(string input) => Task.FromResult(input.Length);
                  public virtual IList<int> GetItems() => new List<int>();
                  public virtual Task<int> Value { get; set; } = Task.FromResult(0);
              }

              internal class UnitTest
              {
                  private static int GetInt() => 42;
                  private static string GetString() => "hello";
                  private static Task<int> GetIntAsync() => Task.FromResult(42);

                  // Non-MemberAccess invocation exercises analyzer's early-exit path
                  private static int CallHelper() => GetInt();

                  private void Test()
                  {
                      {{mock}}
                  }
              }
              """;

        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
                source,
                referenceAssemblyGroup,
                configFileName: null,
                configContent: null,
                compilerDiagnostics)
            .ConfigureAwait(false);
    }
}
