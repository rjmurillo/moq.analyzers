using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify that NoSealedClassMocksAnalyzer is properly disabled when configured to be disabled.
/// This ensures that no false warnings are generated when users explicitly disable the analyzer.
///
/// Performance note: When analyzers are disabled via configuration (severity = none),
/// the Roslyn framework automatically avoids calling the analyzer's analysis methods entirely,
/// which provides optimal performance by avoiding unnecessary analysis work.
///
/// Test Coverage:
/// - Pragma warning directives (#pragma warning disable/restore)
/// - Configuration-based disabling via .editorconfig files
/// - Control tests to ensure analyzers work when not disabled.
/// </summary>
public partial class NoSealedClassMocksAnalyzerTests
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
    public async Task ShouldNotReportSealedClassDiagnosticsWhenDisabledWithPragmaWarning(string referenceAssemblyGroup)
    {
        const string source = """
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

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DisabledTestData))]
    public async Task ShouldNotReportSealedClassDiagnosticsWhenDisabledViaConfiguration(string referenceAssemblyGroup)
    {
        const string source = """
                        internal sealed class FooSealed { }

                        internal class UnitTest
                        {
                            private void Test()
                            {
                                new Mock<FooSealed>();
                            }
                        }
                        """;

        const string editorConfig = """
            [*.cs]
            dotnet_diagnostic.Moq1000.severity = none
            """;

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, "/.editorconfig", editorConfig);
    }

    [Theory]
    [MemberData(nameof(DisabledTestData))]
    public async Task ShouldStillReportDiagnosticsWhenNotDisabled(string referenceAssemblyGroup)
    {
        // This test verifies that the analyzers are working correctly when not disabled
        // This is a control test to ensure our disabled tests are meaningful
        const string source = """
                        internal sealed class FooSealed { }

                        internal class UnitTest
                        {
                            private void Test()
                            {
                                new Mock<{|Moq1000:FooSealed|}>();
                            }
                        }
                        """;

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
}
