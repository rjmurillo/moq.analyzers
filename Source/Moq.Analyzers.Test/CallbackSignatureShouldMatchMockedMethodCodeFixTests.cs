namespace Moq.Analyzers.Test
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    [UseReporter(typeof(DiffReporter))]
    public class CallbackSignatureShouldMatchMockedMethodCodeFixTests : CodeFixVerifier
    {
        [Fact]
        public void ShouldSuggestQuickFixIfBadParameters()
        {
            Approvals.Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.cs")));
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CallbackSignatureShouldMatchMockedMethodCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CallbackSignatureShouldMatchMockedMethodAnalyzer();
        }
    }
}