using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

using SetupVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;
using SealedVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify that analyzers are properly disabled when configured to be disabled.
/// This ensures that no false warnings are generated when users explicitly disable analyzers.
/// 
/// Performance note: When analyzers are disabled via configuration (severity = none), 
/// the Roslyn framework should avoid calling the analyzer's analysis methods entirely,
/// which provides optimal performance by avoiding unnecessary analysis work.
/// </summary>
public class DisabledAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Test different ways to disable analyzer diagnostics
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

        output.WriteLine($"Testing: {testName}");
        output.WriteLine(source);

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

        output.WriteLine($"Testing: {testName}");
        output.WriteLine(source);

        await SetupVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportSetupDiagnosticsWhenDisabledViaConfiguration(string referenceAssemblyGroup, string testName)
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
                                new Mock<SampleClass>().Setup(x => x.Property);
                            }
                        }
                        """;

        output.WriteLine($"Testing: {testName}");
        output.WriteLine(source);

        var test = new Test<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer, EmptyCodeFixProvider>
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup],
        };

        // Add .editorconfig to disable the analyzer
        test.TestState.AnalyzerConfigFiles.Add((".editorconfig", """
            [*.cs]
            dotnet_diagnostic.Moq1200.severity = none
            """));

        await test.RunAsync();
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

        output.WriteLine($"Testing: {testName}");
        output.WriteLine(source);

        await SealedVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportSealedClassDiagnosticsWhenDisabledViaConfiguration(string referenceAssemblyGroup, string testName)
    {
        string source = """
                        internal sealed class FooSealed { }

                        internal class UnitTest
                        {
                            private void Test()
                            {
                                new Mock<FooSealed>();
                            }
                        }
                        """;

        output.WriteLine($"Testing: {testName}");
        output.WriteLine(source);

        var test = new Test<NoSealedClassMocksAnalyzer, EmptyCodeFixProvider>
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup],
        };

        // Add .editorconfig to disable the analyzer
        test.TestState.AnalyzerConfigFiles.Add((".editorconfig", """
            [*.cs]
            dotnet_diagnostic.Moq1000.severity = none
            """));

        await test.RunAsync();
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

        output.WriteLine($"Testing control case: {testName}");
        output.WriteLine(source);

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

        output.WriteLine($"Testing global suppression: {testName}");
        output.WriteLine(source);

        await SetupVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
}