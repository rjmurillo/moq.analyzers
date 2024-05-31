namespace Moq.Analyzers.Test
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using VerifyXunit;
    using Xunit;

    public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public Task ShouldPassIfGoodParameters()
        {
            return Verifier.Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SetupShouldBeUsedOnlyForOverridableMembersAnalyzer();
        }
    }
}