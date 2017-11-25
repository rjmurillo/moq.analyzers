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
        public void ShouldPassIfNoParameters()
        {
            VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethodTest/CallbackWithNoParameters.cs"));
        }

        [Fact]
        public void ShouldPassIfGoodParameters()
        {
            VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethodTest/CallbackWithGoodParameters.cs"));
        }

        [Fact]
        public void ShouldFailIfWrongParametersCount()
        {
            var expected = new DiagnosticResult
            {
                Id = "Moq1001",
                Message = String.Format("No mocked methods with this signature."),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 64)
                        }
            };

            VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethodTest/CallbackWithInvalidParametersCount.cs"), expected);

            /*
            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";
            VerifyCSharpFix(test, fixtest); */
        }

        [Fact]
        public void ShouldFailIfMismatchingParameters()
        {
            var expected = new DiagnosticResult
            {
                Id = "Moq1001",
                Message = String.Format("No mocked methods with this signature."),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 64)
                        }
            };

            string source = File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethodTest/CallbackWithMismatchingParameters.cs");
            VerifyCSharpDiagnostic(source, expected);

            Approvals.Verify(VerifyCSharpFix(source));
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