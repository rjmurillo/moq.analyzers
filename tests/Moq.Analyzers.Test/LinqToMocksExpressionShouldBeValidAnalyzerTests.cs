using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.LinqToMocksExpressionShouldBeValidAnalyzer>;

namespace Moq.Analyzers.Test;

public class LinqToMocksExpressionShouldBeValidAnalyzerTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper output = output;

    // Only one version of each static data source method
    // Only one version of each static data source method
    public static IEnumerable<object[]> EdgeCaseExpressionTestData()
    {
        return new object[][]
        {
            ["""Mock.Of<IRepository>(null);"""],
            ["""Mock.Of<IRepository>(r => 42 == 42);"""],
            ["""Mock.Of<IRepository>(r => r != null);"""],

            // new Func<int>(() => 1)() is a non-virtual member, but analyzer does not report a diagnostic
            ["""Mock.Of<IRepository>(r => new Func<int>(() => 1)() == 1);"""],

            // object.Equals is a non-virtual member, analyzer reports Moq1302 at (13,46,13,68)
            ["""Mock.Of<IRepository>(r => {|Moq1302:object.Equals(r, null)|});"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ValidExpressionTestData()
    {
        return new object[][]
        {
            ["""Mock.Of<IRepository>(r => r.IsAuthenticated == true);"""],
            ["""Mock.Of<IRepository>(r => r.Name == "test");"""],
            ["""Mock.Of<IService>(s => s.GetData() == "result");"""],
            ["""Mock.Of<IService>(s => s.Calculate(1, 2) == 3);"""],
            ["""Mock.Of<BaseClass>(b => b.VirtualProperty == "test");"""],
            ["""Mock.Of<BaseClass>(b => b.VirtualMethod() == true);"""],
            ["""Mock.Of<AbstractClass>(a => a.AbstractProperty == 42);"""],
            ["""Mock.Of<AbstractClass>(a => a.AbstractMethod() == "result");"""],
            ["""Mock.Of<IRepository>(r => true);"""],
            ["""Mock.Of<IRepository>(r => false);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidExpressionTestData()
    {
        return new object[][]
        {
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualProperty|} == "test");"""],
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualMethod()|} == true);"""],
            ["""Mock.Of<SealedClass>(s => {|Moq1302:s.Property|} == "value");"""],
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.InstanceMethod()|} == 42);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ComplexExpressionTestData()
    {
        return new object[][]
        {
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.Field|} == 5);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(EdgeCaseExpressionTestData))]
    public async Task ShouldHandleEdgeCaseLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public interface IRepository
              {
                  bool IsAuthenticated { get; set; }
                  string Name { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mockExpression}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ValidExpressionTestData))]
    public async Task ShouldNotReportDiagnosticForValidLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public interface IRepository
              {
                  bool IsAuthenticated { get; set; }
                  string Name { get; set; }
              }

              public interface IService
              {
                  string GetData();
                  int Calculate(int a, int b);
              }

              public class BaseClass
              {
                  public virtual string VirtualProperty { get; set; }
                  public virtual bool VirtualMethod() => true;
              }

              public abstract class AbstractClass
              {
                  public abstract int AbstractProperty { get; set; }
                  public abstract string AbstractMethod();
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mockExpression}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InvalidExpressionTestData))]
    public async Task ShouldReportDiagnosticForInvalidLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public class ConcreteClass
              {
                  public string NonVirtualProperty { get; set; }
                  public bool NonVirtualMethod() => true;
                  public int InstanceMethod() => 42;
              }

              public sealed class SealedClass
              {
                  public string Property { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mockExpression}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ComplexExpressionTestData))]
    public async Task ShouldReportDiagnosticForComplexLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        static string Template(string ns, string mock) =>
            $$"""
              {{ns}}
              using System;

              public class ConcreteClass
              {
                  public int Field;
                  public static int StaticField;
                  public event EventHandler MyEvent;
              }

              public interface IRepository
              {
                  bool IsAuthenticated { get; set; }
              }

              public interface IServiceProvider
              {
                  object GetService(Type serviceType);
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mock}}
                  }
              }
              """;

        string o = Template(@namespace, mockExpression);

        output.WriteLine("Original:");
        output.WriteLine(o);

        await Verifier.VerifyAnalyzerAsync(o, referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldNotAnalyzeNonMockOfInvocations()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            public interface IRepository
            {
                bool IsAuthenticated { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IRepository>();
                    mock.Setup(r => r.IsAuthenticated).Returns(true);
                    
                    // This should not be analyzed by this analyzer
                    var someMethod = SomeMethod(r => r.IsAuthenticated == true);
                }
                
                private string SomeMethod(System.Func<IRepository, bool> predicate) => "test";
            }
            """,
            referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }
}
