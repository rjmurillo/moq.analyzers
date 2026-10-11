using Microsoft.CodeAnalysis.Testing;
using ExplicitVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetExplicitMockBehaviorAnalyzer>;
using StrictVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetStrictMockBehaviorAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests for <see cref="MockBehaviorDiagnosticAnalyzerBase"/> shared logic, exercised through
/// its concrete subclasses <see cref="SetExplicitMockBehaviorAnalyzer"/> and
/// <see cref="SetStrictMockBehaviorAnalyzer"/>.
/// </summary>
/// <remarks>
/// These tests target the base class branches: IsMockReferenced() guard,
/// AnalyzeObjectCreation type guards, and AnalyzeInvocation method guards.
/// Subclass-specific tests live in SetExplicitMockBehaviorAnalyzerTests and
/// SetStrictMockBehaviorAnalyzerTests.
///
/// Not tested: MockBehavior-is-null branch of RegisterCompilationActions in MockBehaviorDiagnosticAnalyzerBase.
/// This guard requires a compilation where Moq.Mock resolves but Moq.MockBehavior does not.
/// That scenario cannot occur with any real Moq assembly.
/// </remarks>
public class MockBehaviorDiagnosticAnalyzerBaseTests
{
    public static IEnumerable<object[]> MoqReferenceAssemblyGroups()
    {
        return new object[][]
        {
            [ReferenceAssemblyCatalog.Net80WithOldMoq],
            [ReferenceAssemblyCatalog.Net80WithNewMoq],
        };
    }

    [Fact]
    public async Task ShouldNotReport_WhenMoqIsNotReferenced()
    {
        const string source = """
            public class Foo
            {
                private void Test() { }
            }
            """;

        // CompilerDiagnostics.None suppresses CS0246 from the global using Moq added by the test infrastructure.
        await VerifyBothAnalyzersAsync(source, ReferenceAssemblyCatalog.Net80, CompilerDiagnostics.None);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotReport_WhenObjectCreationIsNotMockType(string referenceAssemblyGroup)
    {
        const string source = """
            using System.Collections.Generic;

            internal class UnitTest
            {
                private void Test()
                {
                    var list = new List<int>();
                }
            }
            """;

        await VerifyBothAnalyzersAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotReport_WhenInvocationIsNotMockOf(string referenceAssemblyGroup)
    {
        const string source = """
            using System;

            internal class UnitTest
            {
                private void Test()
                {
                    Console.WriteLine("not a mock");
                }
            }
            """;

        await VerifyBothAnalyzersAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotReport_WhenNonMockObjectCreatedWithMoqReferenced(string referenceAssemblyGroup)
    {
        // Moq is referenced but the object creation is not Mock<T> or MockRepository.
        // Exercises the type guard in AnalyzeObjectCreation.
        const string source = """
            using Moq;

            internal class UnitTest
            {
                private void Test()
                {
                    var obj = new object();
                }
            }
            """;

        await VerifyBothAnalyzersAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotReport_WhenMoqInvocationIsNotMockOf(string referenceAssemblyGroup)
    {
        // Moq is referenced, invocation exists on a mock, but it is not Mock.Of<T>().
        // Exercises the invocation method guard in AnalyzeInvocation.
        const string source = """
            using Moq;

            public interface ISample
            {
                void Method();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<ISample>(MockBehavior.Strict);
                    mock.Setup(x => x.Method());
                }
            }
            """;

        await VerifyBothAnalyzersAsync(source, referenceAssemblyGroup);
    }

    [Fact]
    public void GetDiagnosticSubjectName_UnanalyzedOperation_UsesNeutralFallback()
    {
        // The analyzer only reports on Mock<T> and MockRepository creations and Mock.Of<T> invocations,
        // so no source reaches the fallback. Reflection covers it: it must never print "T" or "Unknown".
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation("class C { int F() => 1; }");
        Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax literal = tree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax>()
            .Single();
        IOperation? operation = model.GetOperation(literal);
        Assert.NotNull(operation);

        System.Reflection.MethodInfo? method = typeof(MockBehaviorDiagnosticAnalyzerBase).GetMethod(
            "GetDiagnosticSubjectName",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

#if DEBUG
        System.Reflection.TargetInvocationException exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => method.Invoke(obj: null, [operation]));
        Assert.Equal(
            "Microsoft.VisualStudio.TestPlatform.TestHost.DebugAssertException",
            exception.InnerException?.GetType().FullName);
#else
        Assert.Equal("mock", method.Invoke(obj: null, [operation]));
#endif
    }

    private static async Task VerifyBothAnalyzersAsync(
        string source,
        string referenceAssemblyGroup,
        CompilerDiagnostics? compilerDiagnostics = null)
    {
        await ExplicitVerifier.VerifyAnalyzerAsync(
            source, referenceAssemblyGroup, configFileName: null, configContent: null, compilerDiagnostics).ConfigureAwait(false);
        await StrictVerifier.VerifyAnalyzerAsync(
            source, referenceAssemblyGroup, configFileName: null, configContent: null, compilerDiagnostics).ConfigureAwait(false);
    }
}
