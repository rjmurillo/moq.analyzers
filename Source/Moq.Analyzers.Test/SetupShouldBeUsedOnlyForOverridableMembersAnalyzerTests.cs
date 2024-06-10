using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        foreach (string @namespace in new[] { string.Empty, "namespace MyNamespace;" })
        {
            yield return [@namespace, """new Mock<BaseSampleClass>().Setup(x => {|Moq1200:x.Calculate()|});"""];
            yield return [@namespace, """new Mock<SampleClass>().Setup(x => {|Moq1200:x.Property|});"""];
            yield return [@namespace, """new Mock<SampleClass>().Setup(x => {|Moq1200:x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())|});"""];
            yield return [@namespace, """new Mock<BaseSampleClass>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""];
            yield return [@namespace, """new Mock<ISampleInterface>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""];
            yield return [@namespace, """new Mock<ISampleInterface>().Setup(x => x.TestProperty);"""];
            yield return [@namespace, """new Mock<SampleClass>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""];
            yield return [@namespace, """new Mock<SampleClass>().Setup(x => x.DoSth());"""];
        }
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeSetupForOverridableMembers(string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISampleInterface
                {
                    int Calculate(int a, int b);
                    int TestProperty { get; set; }
                }

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;
                    public abstract int Calculate(int a, int b);
                    public abstract int Calculate(int a, int b, int c);
                }

                public class SampleClass : BaseSampleClass
                {
                    public override int Calculate(int a, int b) => 0;
                    public sealed override int Calculate(int a, int b, int c) => 0;
                    public virtual int DoSth() => 0;
                    public int Property { get; set; }
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
