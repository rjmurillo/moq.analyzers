using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests
{
    [Fact]
    public async Task ShouldFailWhenClassIsSealed()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace NoSealedClassMocks.Sealed;

                internal sealed class FooSealed { }

                internal class Foo { }

                internal class MyUnitTests
                {
                    private void Sealed()
                    {
                        var mock = new Mock<{|Moq1000:FooSealed|}>();
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenClassIsNotSealed()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace NoSealedClassMocks.NotSealed;

                internal sealed class FooSealed { }

                internal class Foo { }

                internal class MyUnitTests
                {
                    private void NotSealed()
                    {
                        var mock = new Mock<Foo>();
                    }
                }
                """);
    }
}
