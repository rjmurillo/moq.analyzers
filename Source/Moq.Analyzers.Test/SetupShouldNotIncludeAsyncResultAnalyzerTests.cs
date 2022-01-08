namespace Moq.Analyzers.Test
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    [UseReporter(typeof(DiffReporter))]
    public class SetupShouldNotIncludeAsyncResultAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public void ShouldPassIfSetupProperly()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupShouldNotIncludeAsyncResult.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SetupShouldNotIncludeAsyncResultAnalyzer();
        }
    }
}
