namespace Moq.Analyzers.Test
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    public class CallbackSignatureShouldMatchMockedMethodCodeFixTests : CodeFixVerifier
    {
        [Fact]
        public Task ShouldSuggestQuickFixIfBadParameters()
        {
            return Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.cs")));
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CallbackSignatureShouldMatchMockedMethodCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CallbackSignatureShouldMatchMockedMethodAnalyzer();
        }
    }
}