using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoConstructorArgumentsForInterfaceMockAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailIfMockedInterfaceHasConstructorParameters()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock.TestBad.cs")));
    }

    [Fact]
    public Task ShouldFailIfMockedInterfaceHasConstructorParametersAndExplicitMockBehavior()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock.TestBadWithMockBehavior.cs")));
    }

    [Fact]
    public Task ShouldPassIfMockedInterfaceDoesNotHaveConstructorParameters()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock.TestGood.cs")));
    }

    [Fact]
    public Task ShouldPassIfCustomMockClassIsUsed()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock_2.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NoConstructorArgumentsForInterfaceMockAnalyzer();
    }
}
