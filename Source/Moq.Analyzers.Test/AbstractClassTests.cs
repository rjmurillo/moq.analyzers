using Microsoft.CodeAnalysis.Diagnostics;

namespace Moq.Analyzers.Test
{
    using System.IO;

    using TestHelper;

    using Xunit;

    public class AbstractClassTests : DiagnosticVerifier
    {
        [Fact]
        public Task ShouldPassIfGoodParametersAndFailOnTypeMismatch()
        {
            return Verify(VerifyCSharpDiagnostic(
                [
                    File.ReadAllText("Data/AbstractClass.Good.cs"),
                    File.ReadAllText("Data/AbstractClass.Bad.cs")
                ]
            ));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConstructorArgumentsShouldMatchAnalyzer();
        }
    }
}
