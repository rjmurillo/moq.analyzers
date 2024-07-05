using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;
public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> InterfaceTestData()
    {
        IEnumerable<object[]> all = new object[][]
        {
            // Regular code (to make sure we bail out early)
            ["""IFoo foo;"""],

            // Regular
            ["""new Mock<IFoo>(MockBehavior.Default);"""],
            ["""new Mock<IFoo>();"""],
            ["""new Mock<IFoo>{|Moq1001:(MockBehavior.Default, 42)|};"""],
            ["""new Mock<IFoo>{|Moq1001:(42)|};"""],

            // LINQ
            ["""Mock.Of<IFoo>();"""],

            // Repository
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<IFoo>(MockBehavior.Default); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<IFoo>(); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> @new = new object[][]
        {
            // LINQ
            ["""Mock.Of<IFoo>(MockBehavior.Default);"""],   // This is only available in newer versions of
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return all.Union(@new);
    }

    [Theory]
    [MemberData(nameof(InterfaceTestData))]
    public async Task ShouldAnalyzeInterface(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public interface IFoo
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
