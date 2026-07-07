using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetExplicitMockBehaviorAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetExplicitMockBehaviorAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        // new Mock<T>() and MockRepository patterns work with all Moq versions
        IEnumerable<object[]> common = new object[][]
        {
            // new Mock<T>() patterns
            ["""{|Moq1400:new Mock<ISample>()|};"""],
            ["""{|Moq1400:new Mock<ISample>(MockBehavior.Default)|};"""],
            ["""{|Moq1400:new Mock<ISample>(MockBehavior.Default | MockBehavior.Strict)|};"""],
            ["""new Mock<ISample>(MockBehavior.Loose);"""],
            ["""new Mock<ISample>(MockBehavior.Strict);"""],
            ["""MockBehavior GetBehavior() => MockBehavior.Strict; MockBehavior behavior = GetBehavior(); new Mock<ISample>(behavior);"""],

            // Constructor arguments with and without an explicit behavior
            ["""new Mock<Foo>(MockBehavior.Loose, 1, 2);"""],
            ["""{|Moq1400:new Mock<Foo>(1, 2)|};"""],
            ["""{|Moq1400:new Mock<Foo>(MockBehavior.Default, 1, 2)|};"""],

            // MockRepository patterns (AnalyzeObjectCreation path)
            ["""{|Moq1400:new MockRepository(MockBehavior.Default)|};"""],
            ["""new MockRepository(MockBehavior.Loose);"""],
            ["""new MockRepository(MockBehavior.Strict);"""],

            // MockRepository.Create<T>() is not analyzed today (no diagnostic, even when the
            // behavior argument is MockBehavior.Default) - these rows pin the current behavior.
            ["""var repository = new MockRepository(MockBehavior.Strict); repository.Create<ISample>();"""],
            ["""var repository = new MockRepository(MockBehavior.Strict); repository.Create<ISample>(MockBehavior.Default);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        // Mock.Of<T>(MockBehavior) was added in Moq 4.12.0
        IEnumerable<object[]> newMoqOnly = new object[][]
        {
            // Mock.Of<T>() patterns (AnalyzeInvocation path)
            ["""{|Moq1400:Mock.Of<ISample>()|};"""],
            ["""{|Moq1400:Mock.Of<ISample>(MockBehavior.Default)|};"""],
            ["""Mock.Of<ISample>(MockBehavior.Loose);"""],
            ["""Mock.Of<ISample>(MockBehavior.Strict);"""],

            // Mock.Of<T>(predicate) overloads
            ["""{|Moq1400:Mock.Of<ISample>(s => s.Name == "x")|};"""],
            ["""Mock.Of<ISample>(s => s.Name == "x", MockBehavior.Loose);"""],
            ["""{|Moq1400:Mock.Of<ISample>(s => s.Name == "x", MockBehavior.Default)|};"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return common.Concat(newMoqOnly);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeExplicitMockBehavior(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISample
                {
                    void Method();
                    string Name { get; set; }
                }

                public class Foo
                {
                    public Foo(int a, int b) { }

                    public Foo(MockBehavior behavior, int a, int b) { }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Incomplete member access inside the creation argument must not crash the analyzer
    /// and produces no diagnostic (current behavior).
    /// </summary>
    /// <param name="referenceAssemblyGroup">The Moq version reference assembly group.</param>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("Net80WithOldMoq")]
    [InlineData("Net80WithNewMoq")]
    public async Task ShouldNotCrashOnIncompleteMockCreation(string referenceAssemblyGroup)
    {
        const string source = """
            public interface ISample
            {
                void Method();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<ISample>(MockBehavior.
                }
            }
            """;

        // CompilerDiagnostics.None suppresses CS1001/CS1026/CS1002/CS0117 from the incomplete code.
        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ShouldReportWhenConstructorUsesDefaultMockBehaviorParameter()
    {
        const string source = """
            namespace Moq
            {
                public enum MockBehavior
                {
                    Strict = 0,
                    Loose = 1,
                    Default = 1,
                }

                public class Mock<T>
                {
                    public Mock(MockBehavior behavior = MockBehavior.Default) { }
                }
            }

            public interface ISample
            {
                void Method();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {|Moq1400:new Moq.Mock<ISample>()|};
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80);
    }

    [Fact]
    public async Task ShouldNotReportWhenConstructorHasNoMockBehaviorOverload()
    {
        const string source = """
            namespace Moq
            {
                public enum MockBehavior
                {
                    Strict = 0,
                    Loose = 1,
                    Default = 1,
                }

                public class Mock<T>
                {
                    public Mock(int value) { }
                }
            }

            public interface ISample
            {
                void Method();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Moq.Mock<ISample>(1);
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80);
    }

    [Fact]
    public void TryHandleMissingMockBehaviorParameter_MissingMockBehaviorSymbol_ReturnsFalse()
    {
        (SemanticModel model, _) = CompilationHelper.CreateCompilation("class C { }");
        SetExplicitMockBehaviorAnalyzer analyzer = new SetExplicitMockBehaviorAnalyzer();
        System.Reflection.MethodInfo? method = typeof(MockBehaviorDiagnosticAnalyzerBase).GetMethod(
            "TryHandleMissingMockBehaviorParameter",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        Type knownSymbolsType = method.GetParameters()[2].ParameterType;
        object knownSymbols = knownSymbolsType
            .GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                binder: null,
                [typeof(Compilation)],
                modifiers: null)!
            .Invoke([model.Compilation]);

#pragma warning disable ECS0900 // Reflection boxes OperationAnalysisContext to cover the unreachable guard path.
#if DEBUG
        System.Reflection.TargetInvocationException exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => method!.Invoke(
                analyzer,
                [default(Microsoft.CodeAnalysis.Diagnostics.OperationAnalysisContext), null, knownSymbols, null, null]));
        Assert.Equal(
            "Microsoft.VisualStudio.TestPlatform.TestHost.DebugAssertException",
            exception.InnerException?.GetType().FullName);
#else
        object? result = method!.Invoke(
            analyzer,
            [default(Microsoft.CodeAnalysis.Diagnostics.OperationAnalysisContext), null, knownSymbols, null, null]);
        Assert.False(Assert.IsType<bool>(result));
#endif
#pragma warning restore ECS0900
    }
}
