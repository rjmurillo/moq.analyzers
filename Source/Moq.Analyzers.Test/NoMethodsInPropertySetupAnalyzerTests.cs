namespace Moq.Analyzers.Test
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using VerifyXunit;
    using Xunit;

    public class NoMethodsInPropertySetupAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public Task Test()
        {
            return Verifier.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoMethodsInPropertySetup.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NoMethodsInPropertySetupAnalyzer();
        }
    }
}