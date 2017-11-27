namespace Moq.Analyzers.Test
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    [UseReporter(typeof(DiffReporter))]
    public class ConstructorArgumentsShouldMatchAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public void ShouldFailIfClassParametersDoNotMatch()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/ConstructorArgumentsShouldMatch.cs")));
        }

        [Fact]
        public void ShouldPassIfCustomMockClassIsUsed()
        {
           // Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/MockInterfaceWithParametersCustomMockFile.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConstructorArgumentsShouldMatchAnalyzer();
        }
    }
}