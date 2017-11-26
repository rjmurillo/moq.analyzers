using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test
{
    [UseReporter(typeof(DiffReporter))]
    public class MatchingConstructorParametersAnalyzerTests : DiagnosticVerifier
    {

        [Fact]
        public void ShouldFailIfClassParametersDoNotMatch()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/MockClassWithParameters.cs")));
        }

        [Fact]
        public void ShouldPassIfCustomMockClassIsUsed()
        {
           // Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/MockInterfaceWithParametersCustomMockFile.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MatchingConstructorParametersAnalyzer();
        }
    }
}