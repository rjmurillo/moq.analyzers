using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> ClassWithDefaultParamCtorTestData()
    {
        IEnumerable<object[]> all = new object[][]
        {
            // Regular
            ["""new Mock<ClassWithDefaultParamCtor>(MockBehavior.Default);"""],
            ["""new Mock<ClassWithDefaultParamCtor>();"""],
            ["""new Mock<ClassWithDefaultParamCtor>(MockBehavior.Default, 21);"""],
            ["""new Mock<ClassWithDefaultParamCtor>(21);"""],

            // LINQ
            ["""Mock.Of<ClassWithDefaultParamCtor>();"""],

            // Repository
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(MockBehavior.Default); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(MockBehavior.Default, 21); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(21); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> @new = new object[][]
        {
            // LINQ
            ["""Mock.Of<ClassWithDefaultParamCtor>(MockBehavior.Default);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return all.Union(@new);
    }

    [Theory]
    [MemberData(nameof(ClassWithDefaultParamCtorTestData))]
    public async Task ShouldAnalyzeClassWithDefaultParamCtor(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              internal class ClassWithDefaultParamCtor
              {
                  public ClassWithDefaultParamCtor(int a = 42) { }
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
