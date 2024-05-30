using Microsoft.CodeAnalysis.Diagnostics;

namespace Moq.Analyzers.Test
{
    using System.IO;

    using ApprovalTests;
    using ApprovalTests.Reporters;

    using TestHelper;

    using Xunit;


    [UseReporter(typeof(DiffReporter))]
    public class AbstractClassTests : DiagnosticVerifier
    {
        [Fact]
        public void ShouldPassIfGoodParameters()
        {
            Approvals.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/AbstractClass.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConstructorArgumentsShouldMatchAnalyzer();
        }
    }
}
