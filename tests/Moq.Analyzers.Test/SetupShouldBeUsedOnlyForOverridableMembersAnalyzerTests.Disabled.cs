using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify that SetupShouldBeUsedOnlyForOverridableMembersAnalyzer is properly disabled when configured to be disabled.
/// This ensures that no false warnings are generated when users explicitly disable the analyzer.
///
/// Performance note: When analyzers are disabled via configuration (severity = none),
/// the Roslyn framework automatically avoids calling the analyzer's analysis methods entirely,
/// which provides optimal performance by avoiding unnecessary analysis work.
///
/// Test Coverage:
/// - Pragma warning directives (#pragma warning disable/restore)
/// - SuppressMessage attributes at method and assembly level
/// - Configuration-based disabling via .editorconfig files.
/// </summary>
public partial class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests
{
    /// <summary>
    /// Test data that provides both old and new Moq reference assembly groups
    /// to ensure disabled analyzer behavior works across different Moq versions.
    /// </summary>
    /// <returns>Test data for both old and new Moq reference assembly groups.</returns>
    public static IEnumerable<object[]> DisabledTestData()
    {
        return new object[][]
        {
            // Empty array - WithMoqReferenceAssemblyGroups will add the reference assembly group
            [],
        }.WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(DisabledTestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledWithPragmaWarning(string referenceAssemblyGroup)
    {
        const string source = """
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

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DisabledTestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledWithSuppressMessage(string referenceAssemblyGroup)
    {
        const string source = """
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

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DisabledTestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledGlobally(string referenceAssemblyGroup)
    {
        const string source = """
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

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DisabledTestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledViaConfiguration(string referenceAssemblyGroup)
    {
        const string source = """
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

        const string editorConfig = """
            [*.cs]
            dotnet_diagnostic.Moq1200.severity = none
            """;

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, "/.editorconfig", editorConfig);
    }
}
