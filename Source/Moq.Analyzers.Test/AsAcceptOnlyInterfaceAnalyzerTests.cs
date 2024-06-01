using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class AsAcceptOnlyInterfaceAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldPassIfGoodParameters()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/AsAcceptOnlyInterface.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new AsShouldBeUsedOnlyForInterfaceAnalyzer();
    }
}
