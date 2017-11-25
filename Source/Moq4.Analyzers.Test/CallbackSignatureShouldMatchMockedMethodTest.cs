using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.IO;
using TestHelper;
using Xunit;

namespace Moq4.Analyzers.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class CallbackSignatureShouldMatchMockedMethodTest : CodeFixVerifier
    {

        [Fact]
        public void ShouldPassIfGoodParameters()
        {
            VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethodTest/CallbackWithGoodParameters.cs"));
        }

        [Fact]
        public void ShouldFailIfBadParameters()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethodTest/CallbackWithBadParameters.cs")));
        }

        [Fact]
        public void ShouldSuggestQuickFixIfBadParameters()
        {
            Approvals.Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethodTest/CallbackWithBadParameters.cs")));
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new FixCallbackSignatureCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CallbackSignatureShouldMatchMockedMethod();
        }
    }
}