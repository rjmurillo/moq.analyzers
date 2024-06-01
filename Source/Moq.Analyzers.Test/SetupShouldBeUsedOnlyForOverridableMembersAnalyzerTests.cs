using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test
{
    public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public Task ShouldPassIfGoodParameters()
        {
            return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupOnlyForOverridableMembers.cs")));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SetupShouldBeUsedOnlyForOverridableMembersAnalyzer();
        }
    }
}
