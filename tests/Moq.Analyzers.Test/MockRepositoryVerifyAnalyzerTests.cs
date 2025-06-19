using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.MockRepositoryVerifyAnalyzer>;

namespace Moq.Analyzers.Test;

public class MockRepositoryVerifyAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> testCases = new object[][]
        {
            // Should NOT report diagnostic - Verify() is called
            [
                """
                var repository = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                repository.Verify();
                """,
            ],

            // Should report diagnostic - Verify() is NOT called
            [
                """
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                """,
            ],

            // Should NOT report diagnostic - No Create() calls
            [
                """
                var repository = new MockRepository(MockBehavior.Strict);
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return testCases;
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMockRepositoryUsage(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        static string Template(string ns, string content) =>
            $$"""
            {{ns}}

            internal interface IFoo
            {
                void DoSomething();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{content}}
                }
            }
            """;

        string code = Template(@namespace, testCode);
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }
}
