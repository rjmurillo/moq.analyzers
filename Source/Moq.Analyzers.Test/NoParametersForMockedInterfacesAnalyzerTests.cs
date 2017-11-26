using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class NoParametersForMockedInterfacesAnalyzerTests : DiagnosticVerifier
    {

        [Fact]
        public void ShouldFailIfMockedInterfaceHasParameters()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/MockInterfaceWithParameters.cs")));
        }

        [Fact]
        public void ShouldPassIfCustomMockClassIsUsed()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/MockInterfaceWithParametersCustomMockFile.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NoParametersForMockedInterfacesAnalyzer();
        }
    }
}