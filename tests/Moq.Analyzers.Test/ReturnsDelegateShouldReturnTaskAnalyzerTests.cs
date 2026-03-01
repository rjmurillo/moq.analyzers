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

            // Non-generic Task method with sync delegate (no mismatch, Task has no inner type)
            ["""new Mock<AsyncService>().Setup(c => c.DoAsync()).Returns(() => Task.CompletedTask);"""],

            // Parenthesized Setup with ReturnsAsync (valid)
            ["""(new Mock<AsyncService>().Setup(c => c.GetValueAsync())).ReturnsAsync(42);"""],

            // Block-bodied lambda returning Task (correct)
            ["""new Mock<AsyncService>().Setup(c => c.GetValueAsync()).Returns(() => { return Task.FromResult(42); });"""],
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
        };

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> DelegateOverloadTestData()
    {
        IEnumerable<object[]> data = new object[][]
        {
            // Sync delegate with parameter returning wrong type on Task<int> method
            ["""new Mock<AsyncService>().Setup(c => c.ProcessAsync(It.IsAny<string>())).{|Moq1208:Returns((string x) => x.Length)|};"""],
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
    [MemberData(nameof(InvalidTestData))]
    public async Task ShouldTriggerOnSyncDelegateMismatch(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(DelegateOverloadTestData))]
    public async Task ShouldFlagSyncDelegateLambdaWithParameterInReturns(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    private async Task VerifyAsync(string referenceAssemblyGroup, string @namespace, string mock)
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
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      {{mock}}
                  }
              }
              """;

        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
                source,
                referenceAssemblyGroup)
            .ConfigureAwait(false);
    }
}
