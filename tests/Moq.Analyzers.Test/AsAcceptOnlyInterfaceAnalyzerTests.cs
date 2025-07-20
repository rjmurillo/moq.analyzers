using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.AsShouldBeUsedOnlyForInterfaceAnalyzer>;

namespace Moq.Analyzers.Test;

public class AsAcceptOnlyInterfaceAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<BaseSampleClass>().As<{|Moq1300:BaseSampleClass|}>();"""],
            ["""new Mock<BaseSampleClass>().As<{|Moq1300:SampleClass|}>();"""],
            ["""new Mock<SampleClass>().As<ISampleInterface>();"""],
            ["""new Mock<SampleClass>().As<ISampleInterface>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns(10);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeAs(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string o = Template(@namespace, mock);
        output.WriteLine("Original:");
        output.WriteLine(o);

        await Verifier.VerifyAnalyzerAsync(o, referenceAssemblyGroup);
        return;

        static string Template(string ns, string m) =>
            $$"""
              {{ns}}

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
                      {{m}}
                  }
              }
              """;
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
