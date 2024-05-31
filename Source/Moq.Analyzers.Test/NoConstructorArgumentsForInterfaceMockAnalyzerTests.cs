namespace Moq.Analyzers.Test
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    public class NoConstructorArgumentsForInterfaceMockAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public Task ShouldFailIfMockedInterfaceHasParameters()
        {
            return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock_1.cs")));
        }

        [Fact]
        public Task ShouldPassIfCustomMockClassIsUsed()
        {
            return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/NoConstructorArgumentsForInterfaceMock_2.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NoConstructorArgumentsForInterfaceMockAnalyzer();
        }
    }
}