using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailWhenClassIsSealed()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
            ]));
    }

    [Fact]
    public Task ShouldPassWhenClassIsNotSealed()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
            ]));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NoSealedClassMocksAnalyzer();
    }
}
