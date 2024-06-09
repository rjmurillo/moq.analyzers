using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoMethodsInPropertySetupAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoMethodsInPropertySetupAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        foreach (string @namespace in new[] { string.Empty, "namespace MyNamespace;" })
        {
            yield return [@namespace, """new Mock<IFoo>().SetupGet(x => x.Prop1);"""];
            yield return [@namespace, """new Mock<IFoo>().SetupGet(x => x.Prop2);"""];
            yield return [@namespace, """new Mock<IFoo>().SetupSet(x => x.Prop1 = "1");"""];
            yield return [@namespace, """new Mock<IFoo>().SetupSet(x => x.Prop3 = "2");"""];
            yield return [@namespace, """new Mock<IFoo>().Setup(x => x.Method());"""];
            yield return [@namespace, """new Mock<IFoo>().SetupGet(x => {|Moq1101:x.Method()|});"""];
            yield return [@namespace, """new Mock<IFoo>().SetupSet(x => {|Moq1101:x.Method()|});"""];
        }
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzePropertySetup(string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface IFoo
                {
                    string Prop1 { get; set; }

                    string Prop2 { get; }

                    string Prop3 { set; }

                    string Method();
                }

                public class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """);
    }
}
