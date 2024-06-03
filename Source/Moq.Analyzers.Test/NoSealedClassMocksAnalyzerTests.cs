using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailIfFileIsSealed()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoSealedClassMocks.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NoSealedClassMocksAnalyzer();
    }
}
