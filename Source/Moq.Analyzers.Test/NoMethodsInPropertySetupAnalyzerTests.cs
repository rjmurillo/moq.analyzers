using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class NoMethodsInPropertySetupAnalyzerTests : DiagnosticVerifier
    {

        [Fact]
        public void Test()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoMethodsInPropertySetup.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NoMethodsInPropertySetupAnalyzer();
        }
    }
}