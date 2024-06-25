using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default)|};"""],
            ["""new Mock<Foo>("3");"""],
            ["""new Mock<Foo>(MockBehavior.Default, "5");"""],
            ["""new Mock<Foo>(false, 0);"""],
            ["""new Mock<Foo>(MockBehavior.Default, true, 1);"""],
            ["""new Mock<Foo>(DateTime.Now, DateTime.Now);"""],
            ["""new Mock<Foo>(MockBehavior.Default, DateTime.Now, DateTime.Now);"""],
            ["""new Mock<Foo>(MockBehavior.Default, new List<string>());"""],
            ["""new Mock<Foo>(new List<string>());"""],
            ["""new Mock<Foo>(MockBehavior.Default, new List<string>(), "8");"""],
            ["""new Mock<Foo>(new List<string>(), "7");"""],
            ["""new Mock<Foo>{|Moq1002:(1, true)|};"""],
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default, 2, true)|};"""],
            ["""new Mock<Foo>{|Moq1002:("1", 3)|};"""],
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default, "2", 6)|};"""],
            ["""new Mock<Foo>{|Moq1002:(new int[] { 1, 2, 3 })|};"""],
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default, 4, true)|};"""],

            ["""new Mock<AbstractGenericClassDefaultCtor<object>>(MockBehavior.Default);"""],
            ["""new Mock<AbstractGenericClassDefaultCtor<object>>();"""],
            ["""new Mock<AbstractGenericClassDefaultCtor<object>>{|Moq1002:(42)|};"""],

            ["""new Mock<AbstractClassDefaultCtor>(MockBehavior.Default);"""],
            ["""new Mock<AbstractClassDefaultCtor>();"""],
            ["""new Mock<AbstractClassDefaultCtor>{|Moq1002:(MockBehavior.Default, 42)|};"""],
            ["""new Mock<AbstractClassDefaultCtor>{|Moq1002:(42)|};"""],

            ["""new Mock<AbstractClassWithDefaultParamCtor>(MockBehavior.Default);"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>();"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>(MockBehavior.Default, 42);"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>(42);"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>{|Moq1002:(MockBehavior.Default, "42")|};"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>{|Moq1002:("42")|};"""],

            ["""new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42);"""],
            ["""new Mock<AbstractClassWithCtor>(42);"""],
            ["""new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42, "42");"""],
            ["""new Mock<AbstractClassWithCtor>(42, "42");"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:("42")|};"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:("42", 42)|};"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:(MockBehavior.Default)|};"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:()|};"""],

            ["""new Mock<AbstractGenericClassWithCtor<object>>(MockBehavior.Default, 42);"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>(42);"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:("42")|};"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:("42", 42)|};"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:()|};"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:(MockBehavior.Default)|};"""],
        }.WithNamespaces().WithReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeConstructorArguments(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal class Foo
                {
                    public Foo(string s) { }
                    public Foo(bool b, int i) { }
                    public Foo(params DateTime[] dates) { }
                    public Foo(List<string> l, string s = "A") { }
                }

                internal abstract class AbstractClassDefaultCtor
                {
                }

                internal abstract class AbstractGenericClassDefaultCtor<T>
                {
                }

                internal abstract class AbstractClassWithDefaultParamCtor
                {
                    protected AbstractClassWithDefaultParamCtor(int a = 42) { }
                }

                internal abstract class AbstractClassWithCtor
                {
                    protected AbstractClassWithCtor(int a) { }
                    protected AbstractClassWithCtor(int a, string b) { }
                }

                internal abstract class AbstractGenericClassWithCtor<T>
                {
                    protected AbstractGenericClassWithCtor(int a) { }
                    protected AbstractGenericClassWithCtor(int a, string b) { }
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
}
