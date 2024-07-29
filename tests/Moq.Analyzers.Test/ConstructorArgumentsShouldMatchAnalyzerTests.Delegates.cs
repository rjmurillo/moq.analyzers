﻿using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> DelegateTestData()
    {
        return new object[][]
        {
            ["""new Mock<DelegateWithParam>();"""], // This is allowed by Moq and doesn't blow up at runtime
            ["""new Mock<DelegateWithoutParam>();"""],
            ["""new Mock<DelegateWithoutParam>{|Moq1002:(42)|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(DelegateTestData))]
    public async Task ShouldAnalyzeDelegate(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public delegate void DelegateWithParam(int a);
              public delegate void DelegateWithoutParam();

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
