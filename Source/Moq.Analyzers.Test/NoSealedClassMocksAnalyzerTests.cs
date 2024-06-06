using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Test.Helpers;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests : DiagnosticVerifier<NoSealedClassMocksAnalyzer>
{
    // [Fact]
    public Task ShouldFailWhenClassIsSealed()
    {
        return Verify(VerifyCSharpDiagnostic(
                """
                using System;
                using Moq;

                namespace NoSealedClassMocks.Sealed;

                internal sealed class FooSealed { }

                internal class Foo { }

                internal class MyUnitTests
                {
                    private void Sealed()
                    {
                        var mock = new Mock<FooSealed>();
                    }
                }
                """
            ));
    }

    // [Fact]
    public Task ShouldPassWhenClassIsNotSealed()
    {
        return Verify(VerifyCSharpDiagnostic(
                """
                using System;
                using Moq;

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
                """
            ));
    }
}
