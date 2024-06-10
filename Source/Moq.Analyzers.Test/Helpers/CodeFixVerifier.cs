using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Moq.Analyzers.Test.Helpers;

internal static class CodeFixVerifier<TAnalyzer, TCodeFixProvider>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixProvider : CodeFixProvider, new()
{
    public static async Task VerifyCodeFixAsync(string originalSource, string fixedSource)
    {
        await new Test<TAnalyzer, TCodeFixProvider>
        {
            TestCode = originalSource,
            FixedCode = fixedSource,
        }.RunAsync().ConfigureAwait(false);
    }
}
