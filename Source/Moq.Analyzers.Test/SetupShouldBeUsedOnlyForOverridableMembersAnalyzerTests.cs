using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailWhenSetupIsCalledWithANonVirtualMethod()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestBadSetupForNonVirtualMethod.cs")));
    }

    [Fact]
    public Task ShouldFailWhenSetupIsCalledWithANonVirtualProperty()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestBadSetupForNonVirtualProperty.cs")));
    }

    [Fact]
    public Task ShouldFailWhenSetupIsCalledWithASealedMethod()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestBadSetupForSealedMethod.cs")));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnAbstractMethod()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestOkForAbstractMethod.cs")));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnInterfaceMethod()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestOkForInterfaceMethod.cs")));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnInterfaceProperty()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestOkForInterfaceProperty.cs")));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnOverrideOfAnAbstractMethod()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestOkForOverrideAbstractMethod.cs")));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAVirtualMethod()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.TestOkForVirtualMethod.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SetupShouldBeUsedOnlyForOverridableMembersAnalyzer();
    }
}
