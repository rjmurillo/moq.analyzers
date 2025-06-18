using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzers handle custom matcher patterns using Match.Create and [Matcher] attribute.
/// These tests verify that custom argument matchers work properly with analyzers.
/// </summary>
public class CustomMatcherPatternsTests
{
    public static IEnumerable<object[]> CustomMatcherTestData()
    {
        // Test basic custom matcher patterns that should work with both Moq versions
        IEnumerable<object[]> both = new object[][]
        {
            // Basic custom matcher using Match.Create with simple predicate
            ["""new Mock<ISimpleInterface>().Setup(x => x.ProcessString(Match.Create<string>(s => s.StartsWith("test")))).Returns(true);"""],
            ["""new Mock<ISimpleInterface>().Setup(x => x.ProcessNumber(Match.Create<int>(n => n > 0))).Returns(true);"""],

            // Custom matcher with static method
            ["""new Mock<ISimpleInterface>().Setup(x => x.ProcessString(PositiveString())).Returns(true);"""],
            ["""new Mock<ISimpleInterface>().Setup(x => x.ProcessNumber(PositiveNumber())).Returns(true);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return both;
    }

    [Theory]
    [MemberData(nameof(CustomMatcherTestData))]
    public async Task ShouldHandleCustomMatcherPatterns(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISimpleInterface
                {
                    bool ProcessString(string input);
                    bool ProcessNumber(int number);
                }

                internal class UnitTest
                {
                    [Moq.Matcher]
                    public static string PositiveString()
                    {
                        return Match.Create<string>(s => !string.IsNullOrEmpty(s) && s.Length > 0);
                    }

                    [Moq.Matcher]
                    public static int PositiveNumber()
                    {
                        return Match.Create<int>(n => n > 0);
                    }

                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }
}
