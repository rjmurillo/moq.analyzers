namespace Moq.Analyzers.Test
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

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