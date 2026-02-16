using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer>;

namespace Moq.Analyzers.Test;

public class ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        // Valid patterns that should NOT trigger the analyzer
        IEnumerable<object[]> valid = new object[][]
        {
            // Correct usage with ReturnsAsync
            ["""new Mock<AsyncClient>().Setup(c => c.GetAsync()).ReturnsAsync("value");"""],
            ["""new Mock<AsyncClient>().Setup(c => c.GetValueTaskAsync()).ReturnsAsync("value");"""],

            // Correct usage with Returns and sync lambda
            ["""new Mock<AsyncClient>().Setup(c => c.GetAsync()).Returns(() => Task.FromResult("value"));"""],
            ["""new Mock<AsyncClient>().Setup(c => c.DoAsync()).Returns(() => Task.CompletedTask);"""],
            ["""new Mock<AsyncClient>().Setup(c => c.GetValueTaskAsync()).Returns(() => ValueTask.FromResult("value"));"""],
            ["""new Mock<AsyncClient>().Setup(c => c.DoValueTaskAsync()).Returns(() => ValueTask.CompletedTask);"""],

            // Non-async methods should not be affected
            ["""new Mock<AsyncClient>().Setup(c => c.GetSync()).Returns("value");"""],
            ["""new Mock<AsyncClient>().Setup(c => c.GetSync()).Returns(() => "value");"""],

            // Setup without Returns call should not be affected
            ["""new Mock<AsyncClient>().Setup(c => c.GetAsync());"""],

            // Callback chained before Returns: FindSetupInvocation returns null because
            // the expression before .Returns is Callback, not Setup
            ["""new Mock<AsyncClient>().Setup(c => c.GetAsync()).Callback(() => { }).Returns(async () => "value");"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        // Invalid patterns that SHOULD trigger the analyzer
        IEnumerable<object[]> invalid = new object[][]
        {
            // Async lambda in Returns for Task<T> method
            ["""new Mock<AsyncClient>().Setup(c => c.GetAsync()).{|Moq1206:Returns(async () => "value")|};"""],

            // Async lambda in Returns for Task method
            ["""new Mock<AsyncClient>().Setup(c => c.DoAsync()).{|Moq1206:Returns(async () => { })|};"""],

            // Async lambda in Returns for ValueTask<T> method
            ["""new Mock<AsyncClient>().Setup(c => c.GetValueTaskAsync()).{|Moq1206:Returns(async () => "value")|};"""],

            // Async lambda in Returns for ValueTask method
            ["""new Mock<AsyncClient>().Setup(c => c.DoValueTaskAsync()).{|Moq1206:Returns(async () => { })|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return valid.Concat(invalid);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeReturnsAsyncUsage(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source =
            $$"""
              {{@namespace}}

              public class AsyncClient
              {
                  public virtual Task DoAsync() => Task.CompletedTask;
                  public virtual Task<string> GetAsync() => Task.FromResult(string.Empty);
                  public virtual ValueTask DoValueTaskAsync() => ValueTask.CompletedTask;
                  public virtual ValueTask<string> GetValueTaskAsync() => ValueTask.FromResult(string.Empty);
                  public virtual string GetSync() => string.Empty;
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
                referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
