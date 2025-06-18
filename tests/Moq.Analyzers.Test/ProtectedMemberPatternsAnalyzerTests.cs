using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.AsShouldBeUsedOnlyForInterfaceAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzer coverage for protected member patterns from the quickstart guide.
/// This covers Pattern 2 from issue #508: Setup for protected members (Moq.Protected, ItExpr).
/// </summary>
public class ProtectedMemberPatternsAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Pattern 2a: Basic protected setup - should not generate warnings
            ["""
            var mock = new Mock<CommandBase>();
            mock.Protected().Setup<int>("Execute").Returns(5);
            """],

            // Pattern 2b: Protected setup with ItExpr - should not generate warnings (new Moq only)
            ["""
            var mock = new Mock<CommandBase>();
            mock.Protected().Setup<bool>("Execute", ItExpr.IsAny<string>()).Returns(true);
            """],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups().Where(x => x[0]?.ToString()?.Contains("NewMoq") == true);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportDiagnosticsForValidProtectedPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

            {{@namespace}}
            using Moq.Protected;

            public abstract class CommandBase
            {
                protected virtual int Execute() => 0;
                protected virtual bool Execute(string arg) => false;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{testCode}}
                }
            }
            """,
            referenceAssemblyGroup);
    }
}
