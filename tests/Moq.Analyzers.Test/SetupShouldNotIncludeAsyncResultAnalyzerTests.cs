using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldNotIncludeAsyncResultAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetupShouldNotIncludeAsyncResultAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        // Common test cases that behave the same across all Moq versions
        IEnumerable<object[]> common = new object[][]
        {
            ["""new Mock<AsyncClient>().Setup(c => c.TaskAsync());"""],
            ["""new Mock<AsyncClient>().Setup(c => c.GenericTaskAsync()).ReturnsAsync(string.Empty);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        // Old Moq specific: Task<T>.Result should produce diagnostic
        IEnumerable<object[]> oldMoqSpecific = new object[][]
        {
            ["""new Mock<AsyncClient>().Setup(c => {|Moq1201:c.GenericTaskAsync().Result|});"""],

            ["""new Mock<AsyncClient>().Setup(c => {|Moq1201:c.GenericValueTaskAsync().Result|});"""],
        }.WithNamespaces().WithOldMoqReferenceAssemblyGroups();

        // New Moq specific: Task<T>.Result should NOT produce diagnostic
        IEnumerable<object[]> newMoqSpecific = new object[][]
        {
            ["""new Mock<AsyncClient>().Setup(c => c.GenericTaskAsync().Result);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return common.Concat(oldMoqSpecific).Concat(newMoqSpecific);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeSetupForAsyncResult(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source =
            $$"""
              {{@namespace}}

              public class AsyncClient
              {
                  public virtual Task TaskAsync() => Task.CompletedTask;

                  public virtual Task<string> GenericTaskAsync() => Task.FromResult(string.Empty);

                  public virtual ValueTask ValueTaskAsync() => ValueTask.CompletedTask;

                  public virtual ValueTask<string> GenericValueTaskAsync() => ValueTask.FromResult(string.Empty);
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
