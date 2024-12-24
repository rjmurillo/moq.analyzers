﻿using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> ExpressionTestData()
    {
        return new object[][]
        {
            ["""_ = new Mock<Calculator>(() => new Calculator(), MockBehavior.Loose);"""],
            ["""_ = new Mock<Calculator>(() => new Calculator(), MockBehavior.Strict);"""],
            ["""_ = new Mock<Calculator>(() => new Calculator(), MockBehavior.Default);"""],
            ["""_ = new Mock<Calculator>(() => new Calculator());"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ExpressionTestData))]
    public async Task ShouldPassIfExpressionWithDefaultCtorIsUsedWithMockBehavior(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $@"
            {@namespace}
            public class Calculator
            {{
                 public int Add(int a, int b) => a + b;
            }}
            internal class UnitTest
            {{
                private void Test()
                {{
                    {mock}
                }}
            }}
            ",
            referenceAssemblyGroup);
    }
}
