using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class ShouldNotAllowParametersForMockedInterfaceAnalyzerTests : DiagnosticVerifier
    {

        [Fact]
        public void ShouldFailIfFileIsSealed()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/MockInterfaceWithParameters.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ShouldNotAllowParametersForMockedInterfaceAnalyzer();
        }
    }
}