using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.MockGetShouldNotTakeLiteralsAnalyzer>;

namespace Moq.Analyzers.Test;

public class MockGetShouldNotTakeLiteralsAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""Mock.Get<string>({|Moq1301:"literal string"|});"""],
            ["""Mock.Get<ISampleInterface>({|Moq1301:null|});"""],
            ["""Mock.Get<ISampleInterface>({|Moq1301:default(ISampleInterface)|});"""],
            ["""var mock = new Mock<ISampleInterface>(); Mock.Get(mock.Object);"""], // Valid usage
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMockGet(string referenceAssemblyGroup, string @namespace, string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISampleInterface
                {
                    int Calculate(int a, int b);
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mockCode}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }
}
