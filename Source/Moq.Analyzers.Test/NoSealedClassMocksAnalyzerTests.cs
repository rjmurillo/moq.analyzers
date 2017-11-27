namespace Moq.Analyzers.Test
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    [UseReporter(typeof(DiffReporter))]
    public class NoSealedClassMocksAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public void ShouldFailIfFileIsSealed()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoSealedClassMocks.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NoSealedClassMocksAnalyzer();
        }
    }
}