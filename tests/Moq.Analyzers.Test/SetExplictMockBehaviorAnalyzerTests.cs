using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetExplicitMockBehaviorAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetExplictMockBehaviorAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> mockConstructors = new object[][]
        {
            ["""{|Moq1400:new Mock<ISample>()|};"""],
            ["""{|Moq1400:new Mock<ISample>(MockBehavior.Default)|};"""],
            ["""new Mock<ISample>(MockBehavior.Loose);"""],
            ["""new Mock<ISample>(MockBehavior.Strict);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> fluentBuilders = new object[][]
        {
            ["""{|Moq1400:Mock.Of<ISample>()|};"""],
            ["""{|Moq1400:Mock.Of<ISample>(MockBehavior.Default)|};"""],
            ["""Mock.Of<ISample>(MockBehavior.Loose);"""],
            ["""Mock.Of<ISample>(MockBehavior.Strict);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        IEnumerable<object[]> mockRepositories = new object[][]
        {
            ["""{|Moq1400:new MockRepository(MockBehavior.Default)|};"""],
            ["""new MockRepository(MockBehavior.Loose);"""],
            ["""new MockRepository(MockBehavior.Strict);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return mockConstructors.Union(fluentBuilders).Union(mockRepositories);
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
