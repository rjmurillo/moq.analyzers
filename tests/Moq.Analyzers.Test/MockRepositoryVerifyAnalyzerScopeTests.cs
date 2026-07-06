using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.MockRepositoryVerifyAnalyzer>;

namespace Moq.Analyzers.Test;

public class MockRepositoryVerifyAnalyzerScopeTests
{
    private static readonly string ConstructorMissingVerify = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            public UnitTest()
            {
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
            }
        }
        """;

    private static readonly string ConstructorWithVerify = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            public UnitTest()
            {
                var repository = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                repository.Verify();
            }
        }
        """;

    private static readonly string ConstructorWithVerifyAll = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            public UnitTest()
            {
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
                repository.VerifyAll();
            }
        }
        """;

    private static readonly string ConstructorWithVerifyInDifferentMember = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            public UnitTest()
            {
                var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                var fooMock = repository.Create<IFoo>();
            }

            public void VerifyRepository()
            {
                var repository = new MockRepository(MockBehavior.Strict);
                repository.Verify();
            }
        }
        """;

    private static readonly string PropertyAccessorMissingVerify = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            public int Value
            {
                get
                {
                    var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                    var fooMock = repository.Create<IFoo>();
                    return 0;
                }
            }
        }
        """;

    private static readonly string PropertyAccessorWithVerify = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            public int Value
            {
                get
                {
                    var repository = new MockRepository(MockBehavior.Strict);
                    var fooMock = repository.Create<IFoo>();
                    repository.Verify();
                    return 0;
                }
            }
        }
        """;

    private static readonly string LocalFunctionMissingVerify = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            private void Test()
            {
                CreateMock();

                static void CreateMock()
                {
                    var {|Moq1500:repository|} = new MockRepository(MockBehavior.Strict);
                    var fooMock = repository.Create<IFoo>();
                }
            }
        }
        """;

    private static readonly string LocalFunctionWithVerify = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            private void Test()
            {
                CreateMock();

                static void CreateMock()
                {
                    var repository = new MockRepository(MockBehavior.Strict);
                    var fooMock = repository.Create<IFoo>();
                    repository.Verify();
                }
            }
        }
        """;

    private static readonly string FieldInitializerRepositoryCreate = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            private readonly Mock<IFoo> _fooMock = new MockRepository(MockBehavior.Strict).Create<IFoo>();
        }
        """;

    private static readonly string FieldInitializerLambdaRepositoryCreate = """
        internal interface IFoo
        {
            void DoSomething();
        }

        internal class UnitTest
        {
            private readonly Action _init = () =>
            {
                var repository = new MockRepository(MockBehavior.Strict);
                repository.Create<IFoo>();
            };
        }
        """;

    private static readonly string MethodRepositoryDeclaration = """
        using Moq;

        internal class UnitTest
        {
            private void Test()
            {
                var targetRepository = new MockRepository(MockBehavior.Strict);
            }
        }
        """;

    private static readonly string ConstructorRepositoryDeclaration = """
        using Moq;

        internal class UnitTest
        {
            public UnitTest()
            {
                var targetRepository = new MockRepository(MockBehavior.Strict);
            }
        }
        """;

    private static readonly string PropertyAccessorRepositoryDeclaration = """
        using Moq;

        internal class UnitTest
        {
            public int Value
            {
                get
                {
                    var targetRepository = new MockRepository(MockBehavior.Strict);
                    return 0;
                }
            }
        }
        """;

    private static readonly string LocalFunctionRepositoryDeclaration = """
        using Moq;

        internal class UnitTest
        {
            private void Test()
            {
                CreateMock();

                static void CreateMock()
                {
                    var targetRepository = new MockRepository(MockBehavior.Strict);
                }
            }
        }
        """;

    public static IEnumerable<object[]> ScopedRepositoryTestData => GetScopedRepositoryCases()
        .WithNamespaces()
        .WithMoqReferenceAssemblyGroups();

    public static IEnumerable<object[]> OperationRootKindTestData => GetOperationRootKindCases();

    [Theory]
    [MemberData(nameof(ScopedRepositoryTestData))]
    public async Task ShouldAnalyzeRepositoryDeclarationScopes(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string code = BuildNamespacePrefixTemplate(@namespace, testCode);
        await Verifier.VerifyAnalyzerAsync(code, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(OperationRootKindTestData))]
    public async Task ShouldDocumentLocalRepositoryOperationRootKinds(string scenario, string source, string expectedRootKind)
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(source);
        SyntaxNode root = await tree.GetRootAsync();
        VariableDeclaratorSyntax declaratorSyntax = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Single(declarator => string.Equals(declarator.Identifier.ValueText, "targetRepository", StringComparison.Ordinal));

        IOperation? operation = model.GetOperation(declaratorSyntax);
        IVariableDeclaratorOperation declaratorOperation = Assert.IsAssignableFrom<IVariableDeclaratorOperation>(operation);

        Assert.Equal(expectedRootKind, GetRootOperation(declaratorOperation).Kind.ToString());
        Assert.False(string.IsNullOrWhiteSpace(scenario));
    }

    [Fact]
    public async Task ShouldDocumentFieldInitializerDoesNotProduceVariableDeclaratorOperation()
    {
        const string source = """
            using Moq;

            internal interface IFoo
            {
                void DoSomething();
            }

            internal class UnitTest
            {
                private readonly Mock<IFoo> _fooMock = new MockRepository(MockBehavior.Strict).Create<IFoo>();
            }
            """;

        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(source);
        SyntaxNode root = await tree.GetRootAsync();
        VariableDeclaratorSyntax declaratorSyntax = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Single();

        IOperation? operation = model.GetOperation(declaratorSyntax);

        Assert.False(operation is IVariableDeclaratorOperation);
    }

    private static IEnumerable<object[]> GetScopedRepositoryCases()
    {
        yield return [ConstructorMissingVerify];
        yield return [ConstructorWithVerify];
        yield return [ConstructorWithVerifyAll];
        yield return [ConstructorWithVerifyInDifferentMember];
        yield return [PropertyAccessorMissingVerify];
        yield return [PropertyAccessorWithVerify];
        yield return [LocalFunctionMissingVerify];
        yield return [LocalFunctionWithVerify];
        yield return [FieldInitializerRepositoryCreate];
        yield return [FieldInitializerLambdaRepositoryCreate];
    }

    private static IEnumerable<object[]> GetOperationRootKindCases()
    {
        yield return ["method", MethodRepositoryDeclaration, nameof(OperationKind.MethodBody)];
        yield return ["constructor", ConstructorRepositoryDeclaration, nameof(OperationKind.ConstructorBody)];
        yield return ["property accessor", PropertyAccessorRepositoryDeclaration, nameof(OperationKind.MethodBody)];
        yield return ["local function", LocalFunctionRepositoryDeclaration, nameof(OperationKind.MethodBody)];
    }

    private static IOperation GetRootOperation(IOperation operation)
    {
        IOperation current = operation;
        while (current.Parent is not null)
        {
            current = current.Parent;
        }

        return current;
    }

    private static string BuildNamespacePrefixTemplate(string ns, string content) =>
        $$"""
        {{ns}}

        {{content}}
        """;
}
