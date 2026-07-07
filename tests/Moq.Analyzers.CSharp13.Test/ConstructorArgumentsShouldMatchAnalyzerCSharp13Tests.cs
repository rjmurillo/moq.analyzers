using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.CSharp13.Test;

public class ConstructorArgumentsShouldMatchAnalyzerCSharp13Tests
{
    public static IEnumerable<object[]> ParamsCollectionTestData()
    {
        return new object[][]
        {
            ["""new Mock<ClassWithParamsCollection>();"""],
            ["""new Mock<ClassWithParamsCollection>(1, 2, 3);"""],
            ["""new Mock<ClassWithParamsCollection>(MockBehavior.Default, 1, 2, 3);"""],
            ["""new Mock<ClassWithParamsCollection>(new List<int> { 1, 2, 3 });"""],
            ["""new Mock<ClassWithParamsCollection>{|Moq1002:("not an int")|};"""],
        };
    }

    [Theory]
    [MemberData(nameof(ParamsCollectionTestData))]
    public async Task ShouldAnalyzeCSharp13ParamsCollectionConstructors(string mock)
    {
        await VerifyAnalyzerAsync(
            $$"""
              using System;
              using System.Collections.Generic;
              using Moq;

              internal class ClassWithParamsCollection
              {
                  public ClassWithParamsCollection(params List<int> nums) { }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      {{mock}}
                  }
              }
              """);
    }

    private static async Task VerifyAnalyzerAsync(string source)
    {
        CSharpCodeFixTest<ConstructorArgumentsShouldMatchAnalyzer, EmptyCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = ReferenceAssemblyCatalog.Catalog[ReferenceAssemblyCatalog.Net90WithNewMoq],
        };

        await test.RunAsync().ConfigureAwait(false);
    }
}
