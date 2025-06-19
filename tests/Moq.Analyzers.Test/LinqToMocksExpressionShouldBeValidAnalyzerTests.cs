using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.LinqToMocksExpressionShouldBeValidAnalyzer>;

namespace Moq.Analyzers.Test;

public class LinqToMocksExpressionShouldBeValidAnalyzerTests
{
    public static IEnumerable<object[]> ValidExpressionTestData()
    {
        return new object[][]
        {
            // Interface properties - should be valid
            ["""Mock.Of<IRepository>(r => r.IsAuthenticated == true);"""],
            ["""Mock.Of<IRepository>(r => r.Name == "test");"""],

            // Interface methods - should be valid
            ["""Mock.Of<IService>(s => s.GetData() == "result");"""],
            ["""Mock.Of<IService>(s => s.Calculate(1, 2) == 3);"""],

            // Virtual properties - should be valid
            ["""Mock.Of<BaseClass>(b => b.VirtualProperty == "test");"""],

            // Virtual methods - should be valid
            ["""Mock.Of<BaseClass>(b => b.VirtualMethod() == true);"""],

            // Abstract properties - should be valid
            ["""Mock.Of<AbstractClass>(a => a.AbstractProperty == 42);"""],

            // Abstract methods - should be valid
            ["""Mock.Of<AbstractClass>(a => a.AbstractMethod() == "result");"""],

            // Simple boolean expressions without member access
            ["""Mock.Of<IRepository>(r => true);"""],
            ["""Mock.Of<IRepository>(r => false);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidExpressionTestData()
    {
        return new object[][]
        {
            // Non-virtual properties - should trigger diagnostic
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualProperty|} == "test");"""],

            // Non-virtual methods - should trigger diagnostic
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualMethod()|} == true);"""],

            // Sealed class properties
            ["""Mock.Of<SealedClass>(s => {|Moq1302:s.Property|} == "value");"""],

            // Static methods (if they could be called in LINQ expressions)
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.InstanceMethod()|} == 42);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ComplexExpressionTestData()
    {
        return Array.Empty<object[]>().WithNamespaces().WithMoqReferenceAssemblyGroups();
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
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}
              using System;

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
                      var mock = {{mockExpression}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldNotAnalyzeNonMockOfInvocations()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

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
