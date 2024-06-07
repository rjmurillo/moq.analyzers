using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Test.Helpers;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests : DiagnosticVerifier<NoSealedClassMocksAnalyzer>
{
    [Fact]
    public async Task ShouldFailWhenClassIsSealed()
    {
        await VerifyCSharpDiagnostic(
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
                        var mock = new Mock<{|Moq1000:FooSealed|}>();
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenClassIsNotSealed()
    {
        await VerifyCSharpDiagnostic(
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
                """);
    }
}
