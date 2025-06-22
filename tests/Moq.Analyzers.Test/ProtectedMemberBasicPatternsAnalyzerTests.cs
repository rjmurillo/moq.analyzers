namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzer coverage for protected member patterns from the quickstart guide.
/// This covers Pattern 2 from issue #508: Setup for protected members (Moq.Protected).
/// Note: More complex ItExpr patterns would require newer Moq versions and additional setup.
/// </summary>
public class ProtectedMemberBasicPatternsAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Pattern 2a: Basic protected setup - should not generate warnings
            ["""
            var mock = new Mock<CommandBase>(MockBehavior.Strict);
            mock.Protected().Setup<int>("Execute").Returns(5);
            """],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups(); // Only use newer Moq that supports Protected()
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportDiagnosticsForBasicProtectedPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        static string Template(string ns, string tc) =>
            $$"""
              {{ns}}
              using Moq.Protected;

              public abstract class CommandBase
              {
                  protected virtual int Execute() => 0;
                  protected int NonVirtualExecute() => 0;
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      {{tc}}
                  }
              }
              """;

        string o = Template(@namespace, testCode);

        output.WriteLine("Original:");
        output.WriteLine(o);

        await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(o, referenceAssemblyGroup);
    }
}
