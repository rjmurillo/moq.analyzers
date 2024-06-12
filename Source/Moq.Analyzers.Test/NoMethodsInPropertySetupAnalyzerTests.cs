using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoMethodsInPropertySetupAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoMethodsInPropertySetupAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<IFoo>().SetupGet(x => x.Prop1);"""],
            ["""new Mock<IFoo>().SetupGet(x => x.Prop2);"""],
            ["""new Mock<IFoo>().SetupSet(x => x.Prop1 = "1");"""],
            ["""new Mock<IFoo>().SetupSet(x => x.Prop3 = "2");"""],
            ["""new Mock<IFoo>().Setup(x => x.Method());"""],
            ["""new Mock<IFoo>().SetupGet(x => {|Moq1101:x.Method()|});"""],
            ["""new Mock<IFoo>().SetupSet(x => {|Moq1101:x.Method()|});"""],
        }.WithNamespaces().WithReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzePropertySetup(string referenceAssemblyGroup, string @namespace, string mock)
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
                """,
                referenceAssemblyGroup);
    }
}
