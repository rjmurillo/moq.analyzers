using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class ConstructorArgumentsShouldMatchAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailIfClassParametersDoNotMatch()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/ConstructorArgumentsShouldMatch.cs")));
    }

    // [Fact]
    // public Task ShouldPassIfCustomMockClassIsUsed()
    // {
    //    return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/MockInterfaceWithParametersCustomMockFile.cs")));
    // }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ConstructorArgumentsShouldMatchAnalyzer();
    }
}
