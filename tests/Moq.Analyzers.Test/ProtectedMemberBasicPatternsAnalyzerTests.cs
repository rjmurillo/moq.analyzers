using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzer coverage for protected member patterns from the quickstart guide.
/// This covers Pattern 2 from issue #508: Setup for protected members (Moq.Protected).
/// Note: More complex ItExpr patterns would require newer Moq versions and additional setup.
/// </summary>
public class ProtectedMemberBasicPatternsAnalyzerTests
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
        }.WithNamespaces().WithMoqReferenceAssemblyGroups().Where(x => x[0]?.ToString()?.Contains("NewMoq") == true);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportDiagnosticsForBasicProtectedPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

            {{@namespace}}
            using Moq.Protected;

            public abstract class CommandBase
            {
                protected virtual int Execute() => 0;
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
