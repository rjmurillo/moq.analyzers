namespace Moq.Analyzers.Test
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    [UseReporter(typeof(DiffReporter))]
    public class NoConstructorArgumentsForInterfaceMockAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public void ShouldFailIfMockedInterfaceHasParameters()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock_1.cs")));
        }

        [Fact]
        public void ShouldPassIfCustomMockClassIsUsed()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock_2.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NoConstructorArgumentsForInterfaceMockAnalyzer();
        }
    }
}