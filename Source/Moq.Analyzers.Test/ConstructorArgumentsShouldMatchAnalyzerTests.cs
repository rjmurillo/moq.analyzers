namespace Moq.Analyzers.Test
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using VerifyXunit;
    using Xunit;

    public class ConstructorArgumentsShouldMatchAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public Task ShouldFailIfClassParametersDoNotMatch()
        {
            return Verifier.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/ConstructorArgumentsShouldMatch.cs")));
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
}