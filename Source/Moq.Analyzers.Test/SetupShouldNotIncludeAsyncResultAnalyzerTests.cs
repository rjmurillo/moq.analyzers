namespace Moq.Analyzers.Test
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using VerifyXunit;
    using Xunit;

    public class SetupShouldNotIncludeAsyncResultAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public Task ShouldPassIfSetupProperly()
        {
            return Verifier.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupShouldNotIncludeAsyncResult.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SetupShouldNotIncludeAsyncResultAnalyzer();
        }
    }
}
