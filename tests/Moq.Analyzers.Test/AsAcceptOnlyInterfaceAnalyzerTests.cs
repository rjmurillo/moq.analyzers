using Microsoft.CodeAnalysis.Testing;

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

    [Fact]
    public async Task ShouldNotReportForOpenTypeParameter()
    {
        // Moq1300 must not fire on an open type parameter: at the call site T may be
        // constrained to (or substituted with) an interface. See issue #1251.
        await Verifier.VerifyAnalyzerAsync(
            """
            public class SampleClass
            {
                public int Calculate() => 0;
            }

            internal class UnitTest
            {
                private void Test<T>()
                    where T : class
                {
                    _ = new Mock<SampleClass>().As<T>();
                }
            }
            """,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldReportForClassConstrainedTypeParameter()
    {
        // A base-class constraint means T can never be substituted with an interface, so
        // As<T> can never bind to an interface and Moq1300 must still fire. See issue #1251.
        await Verifier.VerifyAnalyzerAsync(
            """
            public abstract class BaseSampleClass
            {
                public int Calculate() => 0;
            }

            internal class UnitTest
            {
                private void Test<T>()
                    where T : BaseSampleClass
                {
                    _ = new Mock<BaseSampleClass>().As<{|Moq1300:T|}>();
                }
            }
            """,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldNotReportForUnresolvedType()
    {
        // An unresolved type argument already produces a compiler error; piling Moq1300 on top
        // is noise, so the analyzer must skip TypeKind.Error. See issue #1251.
        await Verifier.VerifyAnalyzerAsync(
            """
            public class SampleClass
            {
                public int Calculate() => 0;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    _ = new Mock<SampleClass>().As<DoesNotExist>();
                }
            }
            """,
            ReferenceAssemblyCatalog.Net80WithNewMoq,
            CompilerDiagnostics.None);
    }
}
