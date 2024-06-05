using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoMethodsInPropertySetupAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldPassWhenPropertiesUsePropertySetup()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoMethodsInPropertySetup.Good.cs")));
    }

    [Fact]
    public Task ShouldFailWhenMethodsUsePropertySetup()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoMethodsInPropertySetup.Bad.cs")));
    }


    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NoMethodsInPropertySetupAnalyzer();
    }
}
