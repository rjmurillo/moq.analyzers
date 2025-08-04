using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.SetExplicitMockBehaviorAnalyzer, Moq.CodeFixes.SetExplicitMockBehaviorFixer>;

namespace Moq.Analyzers.Test;

public class SetExplicitMockBehaviorCodeFixTests
{
    private readonly ITestOutputHelper _output;

    public SetExplicitMockBehaviorCodeFixTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> mockConstructors = new object[][]
        {
            [
                """{|Moq1400:new Mock<ISample>()|};""",
                """new Mock<ISample>(MockBehavior.Loose);""",
            ],
            [
                """{|Moq1400:new Mock<ISample>(MockBehavior.Default)|};""",
                """new Mock<ISample>(MockBehavior.Loose);""",
            ],
            [
                """new Mock<ISample>(MockBehavior.Loose);""",
                """new Mock<ISample>(MockBehavior.Loose);""",
            ],
            [
                """new Mock<ISample>(MockBehavior.Strict);""",
                """new Mock<ISample>(MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> mockConstructorsWithTargetTypedNew = new object[][]
        {
            [
                """Mock<ISample> mock = {|Moq1400:new()|};""",
                """Mock<ISample> mock = new(MockBehavior.Loose);""",
            ],
            [
                """Mock<ISample> mock = {|Moq1400:new(MockBehavior.Default)|};""",
                """Mock<ISample> mock = new(MockBehavior.Loose);""",
            ],
            [
                """Mock<ISample> mock = new(MockBehavior.Loose);""",
                """Mock<ISample> mock = new(MockBehavior.Loose);""",
            ],
            [
                """Mock<ISample> mock = new(MockBehavior.Strict);""",
                """Mock<ISample> mock = new(MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> mockConstructorsWithExpressions = new object[][]
        {
            [
                """{|Moq1400:new Mock<Calculator>(() => new Calculator())|};""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
            ],
            [
                """{|Moq1400:new Mock<Calculator>(() => new Calculator(), MockBehavior.Default)|};""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
            ],
            [
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);""",
            ],
            [
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Strict);""",
                """new Mock<Calculator>(() => new Calculator(), MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        IEnumerable<object[]> fluentBuilders = new object[][]
        {
            [
                """{|Moq1400:Mock.Of<ISample>()|};""",
                """Mock.Of<ISample>(MockBehavior.Loose);""",
            ],
            [
                """{|Moq1400:Mock.Of<ISample>(MockBehavior.Default)|};""",
                """Mock.Of<ISample>(MockBehavior.Loose);""",
            ],
            [
                """Mock.Of<ISample>(MockBehavior.Loose);""",
                """Mock.Of<ISample>(MockBehavior.Loose);""",
            ],
            [
                """Mock.Of<ISample>(MockBehavior.Strict);""",
                """Mock.Of<ISample>(MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        IEnumerable<object[]> mockRepositories = new object[][]
        {
            [
                """{|Moq1400:new MockRepository(MockBehavior.Default)|};""",
                """new MockRepository(MockBehavior.Loose);""",
            ],
            [
                """new MockRepository(MockBehavior.Loose);""",
                """new MockRepository(MockBehavior.Loose);""",
            ],
            [
                """new MockRepository(MockBehavior.Strict);""",
                """new MockRepository(MockBehavior.Strict);""",
            ],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return mockConstructors.Union(mockConstructorsWithTargetTypedNew).Union(mockConstructorsWithExpressions).Union(fluentBuilders).Union(mockRepositories);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMocksWithoutExplicitMockBehavior(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        static string Template(string ns, string mock) =>
            $$"""
            {{ns}}

            public interface ISample
            {
                int Calculate(int a, int b);
            }

            public class Calculator
            {
                public int Calculate(int a, int b)
                {
                    return a + b;
                }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """;

        string o = Template(@namespace, original);
        string f = Template(@namespace, quickFix);

        _output.WriteLine("Original:");
        _output.WriteLine(o);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Fixed:");
        _output.WriteLine(f);

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
    }

    // The following tests were removed because the early return paths in RegisterCodeFixesAsync
    // (e.g., when TryGetEditProperties returns false or nodeToFix is null) cannot be triggered
    // via the public analyzer/codefix APIs or test harness. These paths are not testable without
    // breaking encapsulation or using unsupported reflection/mocking of Roslyn internals.
}

    [Fact]
    public async Task ShouldHandleNestedMockConstructors()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var outer = {|Moq1400:new Mock<ISample>()|};
                    var inner = {|Moq1400:new Mock<ISample>()|};
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var outer = new Mock<ISample>(MockBehavior.Loose);
                    var inner = new Mock<ISample>(MockBehavior.Loose);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockConstructorsInFieldDeclarations()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private readonly Mock<ISample> _mock = {|Moq1400:new Mock<ISample>()|};
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private readonly Mock<ISample> _mock = new Mock<ISample>(MockBehavior.Loose);
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockConstructorsInPropertyDeclarations()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                public Mock<ISample> MockProperty { get; } = {|Moq1400:new Mock<ISample>()|};
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                public Mock<ISample> MockProperty { get; } = new Mock<ISample>(MockBehavior.Loose);
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMultipleMockConstructorsInSameMethod()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            public interface IOther
            {
                string Process(string input);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock1 = {|Moq1400:new Mock<ISample>()|};
                    var mock2 = {|Moq1400:new Mock<IOther>(MockBehavior.Default)|};
                    var mock3 = new Mock<ISample>(MockBehavior.Strict);
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            public interface IOther
            {
                string Process(string input);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock1 = new Mock<ISample>(MockBehavior.Loose);
                    var mock2 = new Mock<IOther>(MockBehavior.Loose);
                    var mock3 = new Mock<ISample>(MockBehavior.Strict);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockConstructorsInMethodParameters()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    ProcessMock({|Moq1400:new Mock<ISample>()|});
                }
                
                private void ProcessMock(Mock<ISample> mock) { }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    ProcessMock(new Mock<ISample>(MockBehavior.Loose));
                }
                
                private void ProcessMock(Mock<ISample> mock) { }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockConstructorsInReturnStatements()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private Mock<ISample> CreateMock()
                {
                    return {|Moq1400:new Mock<ISample>()|};
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private Mock<ISample> CreateMock()
                {
                    return new Mock<ISample>(MockBehavior.Loose);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleGenericMockConstructors()
    {
        const string original = """
            using Moq;
            using System.Collections.Generic;
            
            public interface IGenericSample<T>
            {
                T Process(T input);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var stringMock = {|Moq1400:new Mock<IGenericSample<string>>()|};
                    var intMock = {|Moq1400:new Mock<IGenericSample<int>>(MockBehavior.Default)|};
                    var listMock = {|Moq1400:new Mock<IGenericSample<List<string>>>()|};
                }
            }
            """;

        const string quickFix = """
            using Moq;
            using System.Collections.Generic;
            
            public interface IGenericSample<T>
            {
                T Process(T input);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var stringMock = new Mock<IGenericSample<string>>(MockBehavior.Loose);
                    var intMock = new Mock<IGenericSample<int>>(MockBehavior.Loose);
                    var listMock = new Mock<IGenericSample<List<string>>>(MockBehavior.Loose);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockConstructorsInConditionalExpressions()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test(bool condition)
                {
                    var mock = condition ? {|Moq1400:new Mock<ISample>()|} : {|Moq1400:new Mock<ISample>(MockBehavior.Default)|};
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test(bool condition)
                {
                    var mock = condition ? new Mock<ISample>(MockBehavior.Loose) : new Mock<ISample>(MockBehavior.Loose);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockConstructorsInArrayInitializers()
    {
        const string original = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mocks = new Mock<ISample>[] 
                    { 
                        {|Moq1400:new Mock<ISample>()|}, 
                        {|Moq1400:new Mock<ISample>(MockBehavior.Default)|},
                        new Mock<ISample>(MockBehavior.Strict)
                    };
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mocks = new Mock<ISample>[] 
                    { 
                        new Mock<ISample>(MockBehavior.Loose), 
                        new Mock<ISample>(MockBehavior.Loose),
                        new Mock<ISample>(MockBehavior.Strict)
                    };
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockOfWithComplexExpressions()
    {
        const string original = """
            using Moq;
            using System;
            
            public interface ISample
            {
                int Calculate(int a, int b);
                string Name { get; set; }
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock = {|Moq1400:Mock.Of<ISample>(x => x.Name == "Test" && x.Calculate(1, 2) == 3)|};
                }
            }
            """;

        const string quickFix = """
            using Moq;
            using System;
            
            public interface ISample
            {
                int Calculate(int a, int b);
                string Name { get; set; }
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ISample>(x => x.Name == "Test" && x.Calculate(1, 2) == 3, MockBehavior.Loose);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMockRepositoryWithCallbackArguments()
    {
        const string original = """
            using Moq;
            using System;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var repository = {|Moq1400:new MockRepository(MockBehavior.Default)|};
                    repository.CallbackVerification += (sender, args) => Console.WriteLine("Callback");
                }
            }
            """;

        const string quickFix = """
            using Moq;
            using System;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var repository = new MockRepository(MockBehavior.Loose);
                    repository.CallbackVerification += (sender, args) => Console.WriteLine("Callback");
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Theory]
    [InlineData("MockBehavior.Default")]
    [InlineData("")]
    public async Task ShouldHandleVariousDefaultBehaviorScenarios(string behaviorParam)
    {
        string mockCall = string.IsNullOrEmpty(behaviorParam) ? 
            "{|Moq1400:new Mock<ISample>()|}" : 
            $"{{|Moq1400:new Mock<ISample>({behaviorParam})|}}";

        string original = $$"""
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock = {{mockCall}};
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface ISample
            {
                int Calculate(int a, int b);
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<ISample>(MockBehavior.Loose);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleComplexInheritanceScenarios()
    {
        const string original = """
            using Moq;
            
            public interface IBaseInterface
            {
                void BaseMethod();
            }
            
            public interface IDerivedInterface : IBaseInterface
            {
                void DerivedMethod();
            }
            
            public abstract class BaseClass
            {
                public abstract void AbstractMethod();
            }
            
            public class DerivedClass : BaseClass
            {
                public override void AbstractMethod() { }
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var interfaceMock = {|Moq1400:new Mock<IDerivedInterface>()|};
                    var abstractMock = {|Moq1400:new Mock<BaseClass>()|};
                    var concreteMock = {|Moq1400:new Mock<DerivedClass>()|};
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public interface IBaseInterface
            {
                void BaseMethod();
            }
            
            public interface IDerivedInterface : IBaseInterface
            {
                void DerivedMethod();
            }
            
            public abstract class BaseClass
            {
                public abstract void AbstractMethod();
            }
            
            public class DerivedClass : BaseClass
            {
                public override void AbstractMethod() { }
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var interfaceMock = new Mock<IDerivedInterface>(MockBehavior.Loose);
                    var abstractMock = new Mock<BaseClass>(MockBehavior.Loose);
                    var concreteMock = new Mock<DerivedClass>(MockBehavior.Loose);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }

    [Fact]
    public async Task ShouldHandleMixedConstructorParametersAndBehavior()
    {
        const string original = """
            using Moq;
            
            public class Calculator
            {
                private readonly string _name;
                public Calculator(string name) { _name = name; }
                public int Calculate(int a, int b) { return a + b; }
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock1 = {|Moq1400:new Mock<Calculator>("test")|};
                    var mock2 = {|Moq1400:new Mock<Calculator>("test", MockBehavior.Default)|};
                    var mock3 = new Mock<Calculator>("test", MockBehavior.Strict);
                }
            }
            """;

        const string quickFix = """
            using Moq;
            
            public class Calculator
            {
                private readonly string _name;
                public Calculator(string name) { _name = name; }
                public int Calculate(int a, int b) { return a + b; }
            }
            
            internal class UnitTest
            {
                private void Test()
                {
                    var mock1 = new Mock<Calculator>(MockBehavior.Loose, "test");
                    var mock2 = new Mock<Calculator>(MockBehavior.Loose, "test");
                    var mock3 = new Mock<Calculator>("test", MockBehavior.Strict);
                }
            }
            """;

        await Verifier.VerifyCodeFixAsync(original, quickFix);
    }
}
