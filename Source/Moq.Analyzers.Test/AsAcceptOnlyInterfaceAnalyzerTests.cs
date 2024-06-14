using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.AsShouldBeUsedOnlyForInterfaceAnalyzer>;

namespace Moq.Analyzers.Test;

public class AsAcceptOnlyInterfaceAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<BaseSampleClass>().As<{|Moq1300:BaseSampleClass|}>();"""],
            ["""new Mock<BaseSampleClass>().As<{|Moq1300:SampleClass|}>();"""],
            ["""new Mock<SampleClass>().As<ISampleInterface>();"""],
            ["""new Mock<SampleClass>().As<ISampleInterface>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(10);"""],
        }.WithNamespaces().WithReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeAs(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISampleInterface
                {
                    int Calculate(int a, int b);
                }

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;
                }

                public class SampleClass
                {

                    public int Calculate() => 0;
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
