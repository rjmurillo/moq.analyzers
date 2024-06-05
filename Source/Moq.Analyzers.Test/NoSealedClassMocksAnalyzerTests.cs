using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailWhenClassIsSealed()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoSealedClassMocks.Sealed.cs")));
    }

    [Fact]
    public Task ShouldPassWhenClassIsNotSealed()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoSealedClassMocks.NotSealed.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NoSealedClassMocksAnalyzer();
    }
}
