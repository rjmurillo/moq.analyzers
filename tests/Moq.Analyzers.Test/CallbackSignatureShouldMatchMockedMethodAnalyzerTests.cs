using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

public class CallbackSignatureShouldMatchMockedMethodAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });"""],
            ["""new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback({|Moq1100:(int i)|} => { });"""],
            ["""new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>())).Callback((int i, string s) => { });"""],
            ["""new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>())).Callback({|Moq1100:(string s1, int i1)|} => { });"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeCallbackSignature(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface IFoo
                {
                    int Do(string s);
                    int Do(int i, string s);
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

    #region Doppelganger Tests

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    #endregion
}
