using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class ConstructorArgumentsShouldMatchAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldPassWhenConstructorArgumentsMatch()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/ConstructorArgumentsShouldMatch.TestGood.cs")));
    }

    [Fact]
    public Task ShouldFailWhenConstructorArumentsDoNotMatch()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/ConstructorArgumentsShouldMatch.TestBad.cs")));
    }

    [Fact]
    public Task ShouldFailWhenConstructorArumentsWithExplicitMockBehaviorDoNotMatch()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/ConstructorArgumentsShouldMatch.TestBadWithMockBehavior.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ConstructorArgumentsShouldMatchAnalyzer();
    }
}
