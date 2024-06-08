using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.AsShouldBeUsedOnlyForInterfaceAnalyzer>;

namespace Moq.Analyzers.Test;

public class AsAcceptOnlyInterfaceAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        foreach (var @namespace in new[] { string.Empty, "namespace MyNamespace;" })
        {
            // TODO: .As<BaseSampleClass> and .As<SampleClass> feels redundant
            yield return [@namespace, """new Mock<BaseSampleClass>().As<{|Moq1300:BaseSampleClass|}>();"""];
            yield return [@namespace, """new Mock<BaseSampleClass>().As<{|Moq1300:SampleClass|}>();"""];
            yield return [@namespace, """new Mock<SampleClass>().As<ISampleInterface>();"""];
            // TODO: Testing with .Setup() and .Returns() seems unnecessary.
            yield return [@namespace, """new Mock<SampleClass>().As<ISampleInterface>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(10);"""];
        }
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeAs(string @namespace, string mock)
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
                """);
    }
}
