namespace Moq.Analyzers.Test
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using VerifyXunit;
    using Xunit;

    public class CallbackSignatureShouldMatchMockedMethodAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public Task ShouldPassIfGoodParameters()
        {
            return Verifier.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CallbackSignatureShouldMatchMockedMethodAnalyzer();
        }
    }
}