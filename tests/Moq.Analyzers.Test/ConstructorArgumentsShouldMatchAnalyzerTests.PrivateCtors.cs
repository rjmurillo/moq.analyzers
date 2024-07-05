using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

#pragma warning disable SA1601 // Partial elements should be documented
public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    // This is to avoid a Castle System.ArgumentException at runtime
    // Can not instantiate proxy of class: NoConstructorClass.
    // Could not find a parameterless constructor. (Parameter 'constructorArguments')
    public static IEnumerable<object[]> ClassWithNoCtorTestData()
    {
        IEnumerable<object[]> all = new object[][]
        {
            // Traditional
            ["""new Mock<NoConstructorClass>{|Moq1002:(MockBehavior.Default)|};"""],
            ["""new Mock<NoConstructorClass>{|Moq1002:()|};"""],

            // LINQ
            ["""{|Moq1002:Mock.Of<NoConstructorClass>()|};"""],
            ["""{|Moq1002:Mock.Of<NoConstructorClass>(m => true)|};"""],

            // Repository
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<NoConstructorClass>{|Moq1002:(MockBehavior.Default)|}; repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<NoConstructorClass>{|Moq1002:()|}; repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> @new = new object[][]
        {
            ["""{|Moq1002:Mock.Of<NoConstructorClass>(m => true, MockBehavior.Default)|};"""],
            ["""{|Moq1002:Mock.Of<NoConstructorClass>(MockBehavior.Default)|};"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return all.Union(@new);
    }

    [Theory]
    [MemberData(nameof(ClassWithNoCtorTestData))]
    public async Task ShouldAnalyzeClassWithNoCtors(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public class NoConstructorClass
              {
                private NoConstructorClass() { }
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
