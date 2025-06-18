using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.LinqToMocksExpressionShouldBeValidAnalyzer>;

namespace Moq.Analyzers.Test;

public class LinqToMocksExpressionShouldBeValidAnalyzerSimpleTests
{
    [Fact]
    public async Task ShouldDetectNonVirtualPropertyInLinqToMocks()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string NonVirtualProperty { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualProperty|} == "test");
                }
            }
            """,
            referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldNotDetectVirtualPropertyInLinqToMocks()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class BaseClass
            {
                public virtual string VirtualProperty { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<BaseClass>(b => b.VirtualProperty == "test");
                }
            }
            """,
            referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }
}
