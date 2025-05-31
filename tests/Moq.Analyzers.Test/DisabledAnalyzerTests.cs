using Microsoft.CodeAnalysis.Testing;

using SetupVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;
using SealedVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

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

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledWithSuppressMessage(string referenceAssemblyGroup, string testName)
    {
        string source = """
                        using System.Diagnostics.CodeAnalysis;

                        public class SampleClass
                        {
                            public int Property { get; set; }
                        }

                        internal class UnitTest
                        {
                            [SuppressMessage("Moq", "Moq1200:Setup should be used only for overridable members")]
                            private void Test()
                            {
                                new Mock<SampleClass>().Setup(x => x.Property);
                            }
                        }
                        """;

        await SetupVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportSealedClassDiagnosticsWhenDisabledWithPragmaWarning(string referenceAssemblyGroup, string testName)
    {
        string source = """
                        internal sealed class FooSealed { }

                        internal class UnitTest
                        {
                            private void Test()
                            {
#pragma warning disable Moq1000
                                new Mock<FooSealed>();
#pragma warning restore Moq1000
                            }
                        }
                        """;

        await SealedVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledGlobally(string referenceAssemblyGroup, string testName)
    {
        string source = """
                        [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Moq", "Moq1200:Setup should be used only for overridable members")]

                        public class SampleClass
                        {
                            public int Property { get; set; }
                        }

                        internal class UnitTest
                        {
                            private void Test()
                            {
                                new Mock<SampleClass>().Setup(x => x.Property);
                            }
                        }
                        """;

        await SetupVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldStillReportDiagnosticsWhenNotDisabled(string referenceAssemblyGroup, string testName)
    {
        // This test verifies that the analyzers are working correctly when not disabled
        // This is a control test to ensure our disabled tests are meaningful
        string source = """
                        internal sealed class FooSealed { }

                        internal class UnitTest
                        {
                            private void Test()
                            {
                                new Mock<{|Moq1000:FooSealed|}>();
                            }
                        }
                        """;

        await SealedVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
}