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

            // Should report diagnostic - VerifyAll() is called instead of Verify()
            // VerifyAll() is not in the tracked Verify methods, so the diagnostic fires
            [
                """
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                repository.VerifyAll();
                """,
            ],

            // Should report diagnostic on repository1 only - Verify() called on different repository
            // Exercises symbol equality check: GetRepositorySymbolFromVerifyCall returns repo2, not repo1
            [
                """
                var {|Moq1500:repository1|} = new MockRepository(MockBehavior.Strict);
                var repository2 = new MockRepository(MockBehavior.Strict);
                var fooMock = repository1.Create<IFoo>();
                repository2.Verify();
                """,
            ],

            // Should NOT report diagnostic - both repositories have Verify() called
            [
                """
                var repository1 = new MockRepository(MockBehavior.Strict);
                var repository2 = new MockRepository(MockBehavior.Strict);
                var fooMock1 = repository1.Create<IFoo>();
                var fooMock2 = repository2.Create<IFoo>();
                repository1.Verify();
                repository2.Verify();
                """,
            ],

            // Should NOT report diagnostic - variable declared without MockRepository initializer
            // Exercises the IsValidMockRepositoryDeclaration false branch
            [
                """
                MockRepository? repository = null;
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
