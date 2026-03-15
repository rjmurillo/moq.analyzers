using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ProtectedSetupShouldUseItExprAnalyzer>;

namespace Moq.Analyzers.Test;

public class ProtectedSetupShouldUseItExprAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> DiagnosticData()
    {
        // IProtectedMock<T> string-based overloads:
        //   Setup(string, params object[]) / Setup<TResult>(string, params object[])
        //   Verify(string, Times, params object[]) / Verify<TResult>(string, Times, params object[])
        //   SetupSet(string, object) / VerifySet(string, Times, object)
        // SetupGet(string) and VerifyGet(string, Times) take no matcher args.
        return new object[][]
        {
            // Setup<TResult> with It.IsAny should diagnose
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsAny<string>()|}).Returns(true);"""],

            // Setup (void) with It.IsAny should diagnose
            ["""mock.Protected().Setup("Bar", {|Moq1600:It.IsAny<string>()|});"""],

            // Setup with multiple args, mix of correct and incorrect
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsAny<string>()|}, ItExpr.IsAny<int>()).Returns(true);"""],

            // Setup with multiple incorrect args
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsAny<string>()|}, {|Moq1600:It.IsAny<int>()|}).Returns(true);"""],

            // Verify with It.IsAny should diagnose
            ["""mock.Protected().Verify("Bar", Times.Once(), {|Moq1600:It.IsAny<string>()|});"""],

            // Verify<TResult> with It.IsAny should diagnose
            ["""mock.Protected().Verify<bool>("Foo", Times.Once(), {|Moq1600:It.IsAny<string>()|});"""],

            // It.Is should also diagnose
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.Is<string>(s => s.Length > 0)|}).Returns(true);"""],

            // SetupSet with It.IsAny should diagnose (value parameter)
            ["""mock.Protected().SetupSet<string>("Baz", {|Moq1600:It.IsAny<string>()|});"""],

            // Setup with exactParameterMatch overload
            ["""mock.Protected().Setup<bool>("Foo", true, {|Moq1600:It.IsAny<string>()|}).Returns(true);"""],

            // VerifySet with It.IsAny should diagnose (value parameter)
            ["""mock.Protected().VerifySet<string>("Baz", Times.Once(), {|Moq1600:It.IsAny<string>()|});"""],

            // It.Is should diagnose (additional It matcher variants)
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsIn<string>("a", "b")|}).Returns(true);"""],

            // It.IsNotIn should diagnose
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsNotIn<string>("a", "b")|}).Returns(true);"""],

            // It.IsRegex should diagnose
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsRegex(".*")|}).Returns(true);"""],

            // It.IsNotNull should diagnose
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsNotNull<string>()|}).Returns(true);"""],

            // It.IsInRange should diagnose
            ["""mock.Protected().Setup<bool>("Foo", {|Moq1600:It.IsInRange<string>("a", "z", Moq.Range.Inclusive)|}).Returns(true);"""],

            // SetupSequence (void) with It.IsAny should diagnose
            ["""mock.Protected().SetupSequence("Bar", {|Moq1600:It.IsAny<string>()|});"""],

            // SetupSequence<TResult> with It.IsAny should diagnose
            ["""mock.Protected().SetupSequence<bool>("Foo", {|Moq1600:It.IsAny<string>()|}).Returns(true).Returns(false);"""],

            // SetupSequence with multiple incorrect args
            ["""mock.Protected().SetupSequence<bool>("Foo", {|Moq1600:It.IsAny<string>()|}, {|Moq1600:It.IsAny<int>()|}).Returns(true);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> NoDiagnosticData()
    {
        return new object[][]
        {
            // ItExpr.IsAny is correct for string-based overloads
            ["""mock.Protected().Setup<bool>("Foo", ItExpr.IsAny<string>()).Returns(true);"""],

            // ItExpr.Is is correct for string-based overloads
            ["""mock.Protected().Setup<bool>("Foo", ItExpr.Is<string>(s => s.Length > 0)).Returns(true);"""],

            // Verify with ItExpr is correct
            ["""mock.Protected().Verify("Bar", Times.Once(), ItExpr.IsAny<string>());"""],

            // Setup without matchers (no arguments after method name)
            ["""mock.Protected().Setup<int>("Execute").Returns(5);"""],

            // As<T>() lambda-based setup with It matchers is correct
            ["""mock.Protected().As<IProtectedBase>().Setup(m => m.DoStuff(It.IsAny<int>())).Returns(42);"""],

            // SetupGet takes only property name, no matchers to check
            ["""mock.Protected().SetupGet<string>("Baz");"""],

            // VerifyGet takes only property name and Times
            ["""mock.Protected().VerifyGet<string>("Baz", Times.Once());"""],

            // VerifySet with ItExpr is correct
            ["""mock.Protected().VerifySet<string>("Baz", Times.Once(), ItExpr.IsAny<string>());"""],

            // SetupSequence with ItExpr.IsAny is correct
            ["""mock.Protected().SetupSequence<bool>("Foo", ItExpr.IsAny<string>()).Returns(true).Returns(false);"""],

            // SetupSequence without matchers (no arguments after method name)
            ["""mock.Protected().SetupSequence<int>("Execute");"""],

            // Literal string value is not a matcher (should not diagnose)
            ["""mock.Protected().Setup<bool>("Foo", "literal").Returns(true);"""],

            // Literal int value is not a matcher (should not diagnose)
            ["""mock.Protected().Setup<bool>("Foo", 42).Returns(true);"""],

            // Literal bool value is not a matcher (should not diagnose)
            ["""mock.Protected().Setup<bool>("Foo", true).Returns(true);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(DiagnosticData))]
    public async Task ShouldDiagnoseItMatcherInProtectedStringSetup(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string source = Template(@namespace, testCode);
        output.WriteLine(source);
        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(NoDiagnosticData))]
    public async Task ShouldNotDiagnoseCorrectProtectedSetup(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        string source = Template(@namespace, testCode);
        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldNotTriggerWhenMoqNotReferenced()
    {
        const string source = """
            public class TestClass
            {
                public void TestMethod()
                {
                    var x = 1 + 2;
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80, CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ShouldNotDiagnoseUserDefinedProtectedClassWithSetup()
    {
        // Doppelganger: a user-defined class named Protected with a string-based Setup method
        // and a user-defined It-like class must not trigger the diagnostic.
        // The analyzer uses symbol equality against Moq.Protected.IProtectedMock<T>, not name matching.
        const string source = """
            using Moq;

            public class MyProtected
            {
                public void Setup(string name, object arg) { }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var p = new MyProtected();
                    p.Setup("Foo", Moq.It.IsAny<string>());
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    private static string Template(string ns, string testCode) =>
$$"""
{{ns}}
using Moq;
using Moq.Protected;

public abstract class MyBase
{
    protected virtual bool Foo(string arg) => false;
    protected virtual bool Foo(string arg, int count) => false;
    protected virtual void Bar(string arg) { }
    protected virtual string Baz { get; set; }
    protected virtual int Execute() => 0;
}

public interface IProtectedBase
{
    int DoStuff(int value);
}

internal class UnitTest
{
    private void Test()
    {
        var mock = new Mock<MyBase>();
        {{testCode}}
    }
}
""";
}
