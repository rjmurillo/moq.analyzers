using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class CallbackSignatureAnalyzerTests : DiagnosticVerifier
    {

        [Fact]
        public void ShouldPassIfGoodParameters()
        {
            VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatures/CallbackWithGoodParameters.cs"));
        }

        [Fact]
        public void ShouldFailIfBadParameters()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatures/CallbackWithBadParameters.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CallbackSignatureAnalyzer();
        }
    }
}