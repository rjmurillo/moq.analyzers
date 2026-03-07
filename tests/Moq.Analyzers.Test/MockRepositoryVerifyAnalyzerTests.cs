using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
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

            // Should NOT report diagnostic - Verify() called with multiple Create() calls
            [
                """
                var repository = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                var barMock = repository.Create<IBar>();
                repository.Verify();
                """,
            ],

            // Should report diagnostic - Multiple Create() calls but no Verify()
            [
                """
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                var barMock = repository.Create<IBar>();
                """,
            ],

            // Should NOT report diagnostic - Verify() called with MockBehavior.Loose
            [
                """
                var repository = new MockRepository(MockBehavior.Loose);
                var fooMock = repository.Create<IFoo>();
                repository.Verify();
                """,
            ],

            // Should report diagnostic - MockBehavior.Loose, Create() but no Verify()
            [
                """
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Loose);
                var fooMock = repository.Create<IFoo>();
                """,
            ],

            // Should NOT report diagnostic - Verify() called with MockBehavior.Default
            [
                """
                var repository = new MockRepository(MockBehavior.Default);
                var fooMock = repository.Create<IFoo>();
                repository.Verify();
                """,
            ],

            // Should report diagnostic - MockBehavior.Default, Create() but no Verify()
            [
                """
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Default);
                var fooMock = repository.Create<IFoo>();
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return testCases;
    }

    public static IEnumerable<object[]> MultipleRepositoryTestData()
    {
        IEnumerable<object[]> testCases = new object[][]
        {
            // Two repositories, both verified - no diagnostics
            [
                """
                var repo1 = new MockRepository(MockBehavior.Strict);
                var repo2 = new MockRepository(MockBehavior.Loose);
                var fooMock = repo1.Create<IFoo>();
                var barMock = repo2.Create<IBar>();
                repo1.Verify();
                repo2.Verify();
                """,
            ],

            // Two repositories, only first verified - diagnostic on second
            [
                """
                var repo1 = new MockRepository(MockBehavior.Strict);
                var {|Moq1500:repo2|} = new MockRepository(MockBehavior.Loose);
                var fooMock = repo1.Create<IFoo>();
                var barMock = repo2.Create<IBar>();
                repo1.Verify();
                """,
            ],

            // Two repositories, only second verified - diagnostic on first
            [
                """
                var {|Moq1500:repo1|} = new MockRepository(MockBehavior.Strict);
                var repo2 = new MockRepository(MockBehavior.Loose);
                var fooMock = repo1.Create<IFoo>();
                var barMock = repo2.Create<IBar>();
                repo2.Verify();
                """,
            ],

            // Two repositories, neither verified - diagnostic on both
            [
                """
                var {|Moq1500:repo1|} = new MockRepository(MockBehavior.Strict);
                var {|Moq1500:repo2|} = new MockRepository(MockBehavior.Loose);
                var fooMock = repo1.Create<IFoo>();
                var barMock = repo2.Create<IBar>();
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return testCases;
    }

    public static IEnumerable<object[]> FieldLevelRepositoryTestData()
    {
        // The analyzer only tracks local variables via ILocalReferenceOperation.
        // Field-level repositories are a known limitation and should not produce diagnostics.
        IEnumerable<object[]> testCases = new object[][]
        {
            // Field-level repository without Verify() - no diagnostic (known limitation)
            [
                """
                internal interface IFoo
                {
                    void DoSomething();
                }

                internal class UnitTest
                {
                    private readonly MockRepository _repository = new MockRepository(MockBehavior.Strict);

                    private void Test()
                    {
                        var fooMock = _repository.Create<IFoo>();
                    }
                }
                """,
            ],

            // Field-level repository with Verify() - no diagnostic
            [
                """
                internal interface IFoo
                {
                    void DoSomething();
                }

                internal class UnitTest
                {
                    private readonly MockRepository _repository = new MockRepository(MockBehavior.Strict);

                    private void Test()
                    {
                        var fooMock = _repository.Create<IFoo>();
                        _repository.Verify();
                    }
                }
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return testCases;
    }

    public static IEnumerable<object[]> ConditionalVerifyTestData()
    {
        IEnumerable<object[]> testCases = new object[][]
        {
            // Verify() inside an if block - no diagnostic (conservative: any call suffices)
            [
                """
                var repository = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                if (true)
                {
                    repository.Verify();
                }
                """,
            ],

            // Verify() inside a try block - no diagnostic
            [
                """
                var repository = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                try
                {
                    repository.Verify();
                }
                catch (System.Exception)
                {
                }
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return testCases;
    }

    public static IEnumerable<object[]> VerifyAllTestData()
    {
        // VerifyAll() is a separate method on MockRepository. The analyzer currently
        // only checks for Verify(), not VerifyAll(). These tests document current behavior.
        IEnumerable<object[]> testCases = new object[][]
        {
            // VerifyAll() called - still reports diagnostic because analyzer only checks Verify()
            [
                """
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                repository.VerifyAll();
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return testCases;
    }

    public static IEnumerable<object[]> SeparateMethodTestData()
    {
        // The analyzer scopes to the containing method body. Verify() in a different
        // method than Create() results in a diagnostic. This documents the limitation.
        IEnumerable<object[]> testCases = new object[][]
        {
            // Create() and Verify() in separate methods - diagnostic because analyzer is method-scoped
            [
                """
                internal interface IFoo
                {
                    void DoSomething();
                }

                internal class UnitTest
                {
                    private void TestCreate()
                    {
                        var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                        var fooMock = repository.Create<IFoo>();
                    }

                    private void TestVerify()
                    {
                        var repository = new MockRepository(MockBehavior.Strict);
                        repository.Verify();
                    }
                }
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return testCases;
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMockRepositoryUsage(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string code = BuildMultiInterfaceTemplate(@namespace, testCode);
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MultipleRepositoryTestData))]
    [SuppressMessage("Blocker Code Smell", "S4144:Methods should not have identical implementations", Justification = "Test methods use different MemberData sources for distinct scenarios")]
    public async Task ShouldAnalyzeMultipleRepositories(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string code = BuildMultiInterfaceTemplate(@namespace, testCode);
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(FieldLevelRepositoryTestData))]
    public async Task ShouldNotReportForFieldLevelRepositories(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string code = @namespace + "\n" + testCode;
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ConditionalVerifyTestData))]
    public async Task ShouldNotReportWhenVerifyIsConditional(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string code = BuildSingleInterfaceTemplate(@namespace, testCode);
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }

    [Theory]
    [InlineData("Net80")]
    public async Task ShouldNotReportWhenMoqIsNotReferenced(string referenceAssemblyGroup)
    {
        const string code = """
            internal interface IFoo
            {
                void DoSomething();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var x = 42;
                }
            }
            """;

        // CompilerDiagnostics.None suppresses CS0246 caused by global using Moq
        // when Moq is not referenced.
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup, CompilerDiagnostics.None);
    }

    [Theory]
    [MemberData(nameof(VerifyAllTestData))]
    [SuppressMessage("Blocker Code Smell", "S4144:Methods should not have identical implementations", Justification = "Test methods use different MemberData sources for distinct scenarios")]
    public async Task ShouldDocumentVerifyAllBehavior(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string code = BuildSingleInterfaceTemplate(@namespace, testCode);
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(SeparateMethodTestData))]
    [SuppressMessage("Blocker Code Smell", "S4144:Methods should not have identical implementations", Justification = "Test methods use different MemberData sources for distinct scenarios")]
    public async Task ShouldReportWhenCreateAndVerifyInSeparateMethods(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string code = @namespace + "\n" + testCode;
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }

    private static string BuildSingleInterfaceTemplate(string ns, string content) =>
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

    private static string BuildMultiInterfaceTemplate(string ns, string content) =>
        $$"""
        {{ns}}

        internal interface IFoo
        {
            void DoSomething();
        }

        internal interface IBar
        {
            int Calculate(int a, int b);
        }

        internal class UnitTest
        {
            private void Test()
            {
                {{content}}
            }
        }
        """;
}
