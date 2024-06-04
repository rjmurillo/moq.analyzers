using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class AbstractClassTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldPassIfGoodParametersAndFailOnTypeMismatch()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                File.ReadAllText("Data/AbstractClass.cs")
            ]
        ));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ConstructorArgumentsShouldMatchAnalyzer();
    }
}
