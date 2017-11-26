using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class CallbackSignatureShouldMatchMockedMethodAnalyzerTests : DiagnosticVerifier
    {

        [Fact]
        public void ShouldPassIfGoodParameters()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CallbackSignatureShouldMatchMockedMethodAnalyzer();
        }
    }
}