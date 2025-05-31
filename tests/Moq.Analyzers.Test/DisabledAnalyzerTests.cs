using Microsoft.CodeAnalysis.Testing;

using SetupVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

public class DisabledAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["Test with pragma warning disable"],
        }.WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledWithPragmaWarning(string referenceAssemblyGroup, string testName)
    {
        string source = """
                        public class SampleClass
                        {
                            public int Property { get; set; }
                        }

                        internal class UnitTest
                        {
                            private void Test()
                            {
#pragma warning disable Moq1200
                                new Mock<SampleClass>().Setup(x => x.Property);
#pragma warning restore Moq1200
                            }
                        }
                        """;

        await SetupVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
}