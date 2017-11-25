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
    public class CallbackSignatureCodeFixTests : CodeFixVerifier
    {

        [Fact]
        public void ShouldSuggestQuickFixIfBadParameters()
        {
            Approvals.Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatures/CallbackWithBadParameters.cs")));
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CallbackSignatureCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CallbackSignatureAnalyzer();
        }
    }
}