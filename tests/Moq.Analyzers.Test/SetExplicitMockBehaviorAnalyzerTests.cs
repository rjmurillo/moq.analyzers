using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetExplicitMockBehaviorAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetExplicitMockBehaviorAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        // new Mock<T>() and MockRepository patterns work with all Moq versions
        IEnumerable<object[]> common = new object[][]
        {
            // new Mock<T>() patterns
            ["""{|Moq1400:new Mock<ISample>()|};"""],
            ["""{|Moq1400:new Mock<ISample>(MockBehavior.Default)|};"""],
            ["""new Mock<ISample>(MockBehavior.Loose);"""],
            ["""new Mock<ISample>(MockBehavior.Strict);"""],

            // MockRepository patterns (AnalyzeObjectCreation path)
            ["""{|Moq1400:new MockRepository(MockBehavior.Default)|};"""],
            ["""new MockRepository(MockBehavior.Loose);"""],
            ["""new MockRepository(MockBehavior.Strict);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        // Mock.Of<T>(MockBehavior) was added in Moq 4.12.0
        IEnumerable<object[]> newMoqOnly = new object[][]
        {
            // Mock.Of<T>() patterns (AnalyzeInvocation path)
            ["""{|Moq1400:Mock.Of<ISample>()|};"""],
            ["""{|Moq1400:Mock.Of<ISample>(MockBehavior.Default)|};"""],
            ["""Mock.Of<ISample>(MockBehavior.Loose);"""],
            ["""Mock.Of<ISample>(MockBehavior.Strict);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return common.Concat(newMoqOnly);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeExplicitMockBehavior(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISample
                {
                    void Method();
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

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
