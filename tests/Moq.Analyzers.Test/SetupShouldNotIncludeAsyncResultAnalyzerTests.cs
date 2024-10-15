using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldNotIncludeAsyncResultAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetupShouldNotIncludeAsyncResultAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<AsyncClient>().Setup(c => c.TaskAsync());"""],

            // Prior to Moq 4.16, the setup helper ReturnsAsync(), ThrowsAsync() are preferred
            ["""new Mock<AsyncClient>().Setup(c => c.GenericTaskAsync()).ReturnsAsync(string.Empty);"""],

            // Starting with Moq 4.16, you can simply .Setup the returned tasks's .Result property
            ["""new Mock<AsyncClient>().Setup(c => {|Moq1201:c.GenericTaskAsync().Result|});"""],
        }.WithNamespaces().WithOldMoqReferenceAssemblyGroups();
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
}
