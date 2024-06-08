using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        foreach (var @namespace in new[] { string.Empty, "namespace MyNamespace;" })
        {
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default);"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Strict);"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Loose);"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default);"""];
            yield return [@namespace, """new Mock<Foo>("3");"""];
            yield return [@namespace, """new Mock<Foo>("4");"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default, "5");"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default, "6");"""];
            yield return [@namespace, """new Mock<Foo>(false, 0);"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default, true, 1);"""];
            yield return [@namespace, """new Mock<Foo>(DateTime.Now, DateTime.Now);"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default, DateTime.Now, DateTime.Now);"""];
            yield return [@namespace, """new Mock<Foo>(new List<string>(), "7");"""];
            yield return [@namespace, """new Mock<Foo>(new List<string>());"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default, new List<string>(), "8");"""];
            yield return [@namespace, """new Mock<Foo>(MockBehavior.Default, new List<string>());"""];
            yield return [@namespace, """new Mock<Foo>{|Moq1002:(1, true)|};"""];
            yield return [@namespace, """new Mock<Foo>{|Moq1002:(2, true)|};"""];
            yield return [@namespace, """new Mock<Foo>{|Moq1002:("1", 3)|};"""];
            yield return [@namespace, """new Mock<Foo>{|Moq1002:(new int[] { 1, 2, 3 })|};"""];
            yield return [@namespace, """new Mock<Foo>{|Moq1002:(MockBehavior.Strict, 4, true)|};"""];
            yield return [@namespace, """new Mock<Foo>{|Moq1002:(MockBehavior.Loose, 5, true)|};"""];
            yield return [@namespace, """new Mock<Foo>{|Moq1002:(MockBehavior.Loose, "2", 6)|};"""];
        }
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeConstructorArguments(string @namespace, string mock)
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

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """);
    }
}
