using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer, Moq.CodeFixes.CallbackSignatureShouldMatchMockedMethodFixer>;

namespace Moq.Analyzers.Test;

public class RefOutCallbackTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Test the original failing case: ref parameter mismatch
            [
                """new Mock<IFoo>().Setup(m => m.DoRef(ref It.Ref<string>.IsAny)).Callback(({|Moq1100:string data|}) => { });""",
                """new Mock<IFoo>().Setup(m => m.DoRef(ref It.Ref<string>.IsAny)).Callback((ref string data) => { });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldHandleRefOutInParameterCallbacks(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        static string Template(string ns, string mock) =>
            $$"""
            {{ns}}

            internal interface IFoo
            {
                int DoRef(ref string data);
                bool DoOut(out int result);
                string DoIn(in DateTime timestamp);
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

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
    }
}
