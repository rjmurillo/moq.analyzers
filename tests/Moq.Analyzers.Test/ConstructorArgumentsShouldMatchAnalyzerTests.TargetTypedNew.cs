using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

#pragma warning disable SA1601 // Partial elements should be documented
public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> TargetTypedNewTestData()
    {
        return new object[][]
        {
            ["""object value = new object();"""],
            ["""List<int> values = new();"""],

            ["""new Mock<IFoo>();"""],
            ["""Mock<IFoo> interfaceMock = new();"""],
            ["""new Mock<IFoo>(MockBehavior.Default);"""],
            ["""Mock<IFoo> interfaceMock = new(MockBehavior.Default);"""],
            ["""new Mock<IFoo>{|Moq1001:(42)|};"""],
            ["""Mock<IFoo> interfaceMock = new{|Moq1001:(42)|};"""],
            ["""new Mock<IFoo>{|Moq1001:(MockBehavior.Default, 42)|};"""],
            ["""Mock<IFoo> interfaceMock = new{|Moq1001:(MockBehavior.Default, 42)|};"""],
            ["""Mock<IFoo> CreateInterfaceMock() => new{|Moq1001:(42)|}; _ = CreateInterfaceMock();"""],

            ["""new Mock<Foo>(false, 0);"""],
            ["""Mock<Foo> classMock = new(false, 0);"""],
            ["""new Mock<Foo>(MockBehavior.Default, false, 0);"""],
            ["""Mock<Foo> classMock = new(MockBehavior.Default, false, 0);"""],
            ["""new Mock<Foo>{|Moq1002:(1, true)|};"""],
            ["""Mock<Foo> classMock = new{|Moq1002:(1, true)|};"""],
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default, 1, true)|};"""],
            ["""Mock<Foo> classMock = new{|Moq1002:(MockBehavior.Default, 1, true)|};"""],
            ["""Mock<Foo> CreateClassMock() => new{|Moq1002:(1, true)|}; _ = CreateClassMock();"""],

            ["""new Mock<ClassDefaultCtor>();"""],
            ["""Mock<ClassDefaultCtor> defaultCtorMock = new();"""],
            ["""new Mock<ClassWithDefaultParamCtor>();"""],
            ["""Mock<ClassWithDefaultParamCtor> defaultParamMock = new();"""],
            ["""new Mock<ClassWithRequiredParamCtor>{|Moq1002:()|};"""],
            ["""Mock<ClassWithRequiredParamCtor> requiredParamMock = new{|Moq1002:()|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TargetTypedNewTestData))]
    public async Task ShouldAnalyzeTargetTypedNewConstructorArguments(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              internal interface IFoo
              {
              }

              internal class Foo
              {
                  public Foo(bool b, int i) { }
              }

              internal class ClassDefaultCtor
              {
              }

              internal class ClassWithDefaultParamCtor
              {
                  public ClassWithDefaultParamCtor(int a = 42) { }
              }

              internal class ClassWithRequiredParamCtor
              {
                  public ClassWithRequiredParamCtor(int a) { }
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
#pragma warning restore SA1601 // Partial elements should be documented
