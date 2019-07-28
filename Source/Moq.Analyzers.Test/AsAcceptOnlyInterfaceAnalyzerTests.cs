namespace Moq.Analyzers.Test
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    [UseReporter(typeof(DiffReporter))]
    public class AsAcceptOnlyInterfaceAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public void ShouldPassIfGoodParameters()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/AsAcceptOnlyInterface.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AsShouldBeUsedOnlyForInterfaceAnalyzer();
        }
    }
}