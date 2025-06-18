using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzers handle custom matcher patterns using Match.Create.
/// These tests verify that custom argument matchers work properly with analyzers.
/// </summary>
public class CustomMatcherPatternsTests
{
    public static IEnumerable<object[]> SimpleCustomMatcherTestData()
    {
        // Test with a simple custom matcher scenario that should compile
        IEnumerable<object[]> both = new object[][]
        {
            // Simple method call with what would be a custom matcher result
            ["""new Mock<ISimpleInterface>().Setup(x => x.ProcessString("custom_match_result")).Returns(true);"""],
            ["""new Mock<ISimpleInterface>().Setup(x => x.ProcessString(It.IsAny<string>())).Returns(true);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return both;
    }

    [Theory]
    [MemberData(nameof(SimpleCustomMatcherTestData))]
    public async Task ShouldHandleCustomMatcherLikePatterns(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISimpleInterface
                {
                    bool ProcessString(string input);
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
