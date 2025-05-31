using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ProtectedMockMemberReferenceShouldUseProtectedSetupAnalyzer>;

namespace Moq.Analyzers.Test;

public class ProtectedMockMemberReferenceShouldUseProtectedSetupAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Should flag: Direct access to protected method
            ["""mock.Setup(x => x.{|Moq1502:ProtectedMethod||}());"""],

            // Should flag: Direct access to protected property
            ["""mock.Setup(x => x.{|Moq1502:ProtectedProperty|}).Returns("value");"""],

            // Should not flag: Public method access
            ["""mock.Setup(x => x.PublicMethod());"""],

            // Should not flag: Public property access
            ["""mock.Setup(x => x.PublicProperty).Returns("value");"""],

            // Should not flag: Protected() method usage (correct pattern)
            ["""mock.Protected().Setup("ProtectedMethod");"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeProtectedMemberAccess(string referenceAssemblyGroup, string @namespace, string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public abstract class BaseClass
                {
                    public virtual void PublicMethod() { }
                    protected virtual void ProtectedMethod() { }
                    public virtual string PublicProperty { get; set; }
                    protected virtual string ProtectedProperty { get; set; }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<BaseClass>();
                        {{mockCode}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }
}
