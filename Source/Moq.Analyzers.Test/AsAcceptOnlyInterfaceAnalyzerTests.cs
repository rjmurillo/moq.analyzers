using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class AsAcceptOnlyInterfaceAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailWhenUsingAsWithAbstractClass()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/AsAcceptOnlyInterface.TestBadAsForAbstractClass.cs")));
    }

    [Fact]
    public Task ShouldFailWhenUsingAsWithConcreteClass()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/AsAcceptOnlyInterface.TestBadAsForNonAbstractClass.cs")));
    }

    [Fact]
    public Task ShouldPassWhenUsingAsWithInterface()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/AsAcceptOnlyInterface.TestOkAsForInterface.cs")));
    }

    [Fact]
    public Task ShouldPassWhenUsingAsWithInterfaceWithSetup()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/AsAcceptOnlyInterface.TestOkAsForInterfaceWithConfiguration.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new AsShouldBeUsedOnlyForInterfaceAnalyzer();
    }
}
