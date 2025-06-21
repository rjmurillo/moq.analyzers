using Moq.Analyzers.Test.Helpers;
using Xunit;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

public class DebugTest
{
    [Fact]
    public async Task TestVerifyProperty()
    {
        string source = """
                        public class SampleClass
                        {
                            public int Property { get; set; }
                        }

                        internal class UnitTest
                        {
                            private void Test()
                            {
                                var mock = new Mock<SampleClass>();
                                {|Moq1210:mock.Verify(x => x.Property)|};
                            }
                        }
                        """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task TestVerifySet()
    {
        string source = """
                        public class SampleClass
                        {
                            public int Property { get; set; }
                        }

                        internal class UnitTest
                        {
                            private void Test()
                            {
                                var mock = new Mock<SampleClass>();
                                {|Moq1210:mock.VerifySet(x => x.Property = It.IsAny<int>())|};
                            }
                        }
                        """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
