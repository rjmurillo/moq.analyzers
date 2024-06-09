using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        foreach (string @namespace in new[] { string.Empty, "namespace MyNamespace;" })
        {
            yield return [@namespace, """new Mock<{|Moq1000:FooSealed|}>();"""];
            yield return [@namespace, """new Mock<Foo>();"""];
        }
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShoulAnalyzeSealedClassMocks(string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal sealed class FooSealed { }

                internal class Foo { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """);
    }
}
