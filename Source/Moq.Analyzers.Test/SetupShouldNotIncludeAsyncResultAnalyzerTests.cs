using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldNotIncludeAsyncResultAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetupShouldNotIncludeAsyncResultAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<AsyncClient>().Setup(c => c.TaskAsync());"""],
            ["""new Mock<AsyncClient>().Setup(c => c.GenericTaskAsync()).ReturnsAsync(string.Empty);"""],
            ["""new Mock<AsyncClient>().Setup(c => {|Moq1201:c.GenericTaskAsync().Result|});"""],
        }.WithNamespaces().WithReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeSetupForAsyncResult(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
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
                """,
                referenceAssemblyGroup);
    }
}
