using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetExplicitMockBehaviorAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetExplictMockBehaviorAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""{|Moq1400:new Mock<ISample>()|};"""],
            ["""{|Moq1400:new Mock<ISample>(MockBehavior.Default)|};"""],
            ["""new Mock<ISample>(MockBehavior.Loose);"""],
            ["""new Mock<ISample>(MockBehavior.Strict);"""],

            ["""{|Moq1400:Mock.Of<ISample>()|};"""],
            ["""{|Moq1400:Mock.Of<ISample>(MockBehavior.Default)|};"""],
            ["""Mock.Of<ISample>(MockBehavior.Loose);"""],
            ["""Mock.Of<ISample>(MockBehavior.Strict);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMocksWithoutExplictMockBehavior(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISample
                {
                    int Calculate(int a, int b);
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
