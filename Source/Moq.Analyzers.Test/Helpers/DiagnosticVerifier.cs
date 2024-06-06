using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

public abstract class DiagnosticVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected async Task VerifyCSharpDiagnostic(string source)
    {
        CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> context = new()
        {
            TestCode = source,
        };

        context.SetDefaults<CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>, DefaultVerifier>();

        await context.RunAsync().ConfigureAwait(false);
    }
}
