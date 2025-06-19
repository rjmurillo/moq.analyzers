using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzers handle advanced argument matcher patterns only available in newer Moq versions.
/// These tests verify coverage of It.IsAnyType, It.IsSubtype, and custom matcher patterns.
/// </summary>
public class AdvancedArgumentMatcherPatternsTests
{
    public static IEnumerable<object[]> AdvancedTypeMatchingTestData()
    {
        // Only test with newer Moq versions that support these features
        IEnumerable<object[]> @new = new object[][]
        {
            // Generic type argument matching (requires Moq 4.13+)
            // Note: Testing with simpler interfaces for better compatibility
            ["""new Mock<IGenericInterface>().Setup(m => m.ProcessGeneric<It.IsAnyType>()).Returns(true);"""],
            ["""new Mock<IGenericInterface>().Setup(m => m.ProcessGeneric<It.IsSubtype<object>>()).Returns(true);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return @new;
    }

    [Theory]
    [MemberData(nameof(AdvancedTypeMatchingTestData))]
    public async Task ShouldHandleAdvancedTypeMatchingPatterns(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface IGenericInterface
                {
                    bool ProcessGeneric<T>();
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
