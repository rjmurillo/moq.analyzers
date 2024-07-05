using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

#pragma warning disable SA1601 // Partial elements should be documented
public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> ClassWithDefaultCtorTestData()
    {
        IEnumerable<object[]> all = new object[][]
        {
            ["""new Mock<ClassDefaultCtor>(MockBehavior.Default);"""],
            ["""new Mock<ClassDefaultCtor>();"""],
            ["""var behavior = MockBehavior.Default; var mock = new Mock<ClassDefaultCtor>(behavior);"""],

            ["""Mock.Of<ClassDefaultCtor>();"""],
            ["""Mock.Of<ClassDefaultCtor>(m => true);"""],

            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassDefaultCtor>(MockBehavior.Default); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassDefaultCtor>(); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> @new = new object[][]
        {
            ["""Mock.Of<ClassDefaultCtor>(m => true, MockBehavior.Default);"""],
            ["""Mock.Of<ClassDefaultCtor>(MockBehavior.Default);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return all.Union(@new);
    }

    [Theory]
    [MemberData(nameof(ClassWithDefaultCtorTestData))]
    public async Task ShouldAnalyzeClassWithDefaultCtor(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public class ClassDefaultCtor
              {
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
