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

            // Generic and nested-generic interface type arguments (should NOT be flagged)
            ["""new Mock<SampleClass>().As<IList<int>>();"""],
            ["""new Mock<SampleClass>().As<IGenericInterface<IList<int>>>();"""],
            ["""new Mock<SampleClass>().As<IDictionary<string, IList<int>>>();"""],

            // Chained As calls, both interfaces (should NOT be flagged)
            ["""new Mock<SampleClass>().As<ISampleInterface>().As<IOtherInterface>();"""],

            // As on a mock variable rather than an inline creation
            ["""var mock = new Mock<SampleClass>(); mock.As<ISampleInterface>();"""],
            ["""var mock = new Mock<SampleClass>(); mock.As<{|Moq1300:SampleClass|}>();"""],

            // Delegate type arguments are not interfaces (flagged; message uses the display string)
            ["""new Mock<SampleClass>().As<{|Moq1300:SampleDelegate|}>();"""],
            ["""new Mock<SampleClass>().As<{|Moq1300:Action<int>|}>();"""],
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

              public interface IOtherInterface
              {
                  void Do();
              }

              public interface IGenericInterface<T>
              {
                  T Get();
              }

              public delegate void SampleDelegate(int x);

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

    /// <summary>
    /// As&lt;T&gt; has a 'where T : class' constraint, so struct/enum type arguments produce CS0452.
    /// Overload resolution fails and the analyzer intentionally stays silent (current behavior).
    /// </summary>
    /// <param name="referenceAssemblyGroup">The Moq version reference assembly group.</param>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("Net80WithOldMoq")]
    [InlineData("Net80WithNewMoq")]
    public async Task ShouldNotReportWhenTypeArgumentViolatesClassConstraint(string referenceAssemblyGroup)
    {
        const string source = """
            public enum SampleEnum { A }
            public struct SampleStruct { public int X; }
            public class SampleClass { }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<SampleClass>().As<SampleStruct>();
                    new Mock<SampleClass>().As<SampleEnum>();
                }
            }
            """;

        // CompilerDiagnostics.None suppresses CS0452 from the class-constraint violation.
        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, CompilerDiagnostics.None);
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
    public async Task ShouldReportForConstructorConstrainedTypeParameter()
    {
        // A new() constraint requires a parameterless constructor, which no interface can
        // satisfy, so T can never be an interface and Moq1300 must still fire. See issue #1251.
        await Verifier.VerifyAnalyzerAsync(
            """
            public class SampleClass
            {
                public int Calculate() => 0;
            }

            internal class UnitTest
            {
                private void Test<T>()
                    where T : class, new()
                {
                    _ = new Mock<SampleClass>().As<{|Moq1300:T|}>();
                }
            }
            """,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
