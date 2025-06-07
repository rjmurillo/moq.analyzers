using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.MockRepositoryVerifyMissingAnalyzer, Moq.CodeFixes.MockRepositoryVerifyMissingFixer>;

namespace Moq.Analyzers.Test;

public class MockRepositoryVerifyMissingCodeFixTests
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> mockRepositoryCreations = new object[][]
        {
            [
                """
                public void TestMethod()
                {
                    var {|Moq1900:repository = new MockRepository(MockBehavior.Strict)|};
                    var mock = repository.Create<ISample>();
                    // Test logic here
                }
                """,
                """
                public void TestMethod()
                {
                    var repository = new MockRepository(MockBehavior.Strict);
                    var mock = repository.Create<ISample>();
                    // Test logic here
                    repository.Verify();
                }
                """,
            ],
            [
                """
                public void TestMethod()
                {
                    var {|Moq1900:repo = new MockRepository(MockBehavior.Default)|};
                    var mock1 = repo.Create<ISample>();
                    var mock2 = repo.Create<IFoo>();
                    // Test logic here
                }
                """,
                """
                public void TestMethod()
                {
                    var repo = new MockRepository(MockBehavior.Default);
                    var mock1 = repo.Create<ISample>();
                    var mock2 = repo.Create<IFoo>();
                    // Test logic here
                    repo.Verify();
                }
                """,
            ],
            [
                """
                public void TestMethod()
                {
                    var {|Moq1900:mockRepository = new MockRepository(MockBehavior.Loose)|};
                    var foo = mockRepository.Create<IFoo>();
                    foo.Setup(x => x.DoSomething()).Returns(42);
                    foo.Object.DoSomething();
                }
                """,
                """
                public void TestMethod()
                {
                    var mockRepository = new MockRepository(MockBehavior.Loose);
                    var foo = mockRepository.Create<IFoo>();
                    foo.Setup(x => x.DoSomething()).Returns(42);
                    foo.Object.DoSomething();
                    mockRepository.Verify();
                }
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return mockRepositoryCreations;
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldSuggestCodeFixForMissingRepositoryVerify(
        string referenceAssemblyGroup,
        string @namespace,
        string original,
        string expected)
    {
        string originalCode = $$"""
            {{@namespace}}
            using Moq;
            
            public interface ISample
            {
                int Calculate();
            }

            public interface IFoo
            {
                int DoSomething();
            }
            
            internal class UnitTest
            {
                {{original}}
            }
            """;

        string expectedCode = $$"""
            {{@namespace}}
            using Moq;
            
            public interface ISample
            {
                int Calculate();
            }

            public interface IFoo
            {
                int DoSomething();
            }
            
            internal class UnitTest
            {
                {{expected}}
            }
            """;

        await Verifier.VerifyCodeFixAsync(originalCode, expectedCode, referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldNotSuggestCodeFixWhenVerifyIsAlreadyCalled()
    {
        const string source = """
            using Moq;
            
            public interface ISample
            {
                int Calculate();
            }
            
            internal class UnitTest
            {
                public void TestMethod()
                {
                    var repository = new MockRepository(MockBehavior.Strict);
                    var mock = repository.Create<ISample>();
                    // Test logic here
                    repository.Verify(); // Already present
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(source, source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
