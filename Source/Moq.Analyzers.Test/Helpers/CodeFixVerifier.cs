using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

public abstract class CodeFixVerifier<TAnalyzer, TCodeFixProvider>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixProvider : CodeFixProvider, new()
{
    protected async Task VerifyCSharpFix(string originalSource, string fixedSource)
    {
        CSharpCodeFixTest<TAnalyzer, TCodeFixProvider, DefaultVerifier> context = new()
        {
            TestCode = originalSource,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblyCatalog.Net80WithOldMoq,
        };

        await context.RunAsync().ConfigureAwait(false);
    }
}
