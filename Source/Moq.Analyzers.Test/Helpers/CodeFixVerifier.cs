using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

internal static class CodeFixVerifier<TAnalyzer, TCodeFixProvider>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixProvider : CodeFixProvider, new()
{
    public static async Task VerifyCodeFixAsync(string originalSource, string fixedSource, string referenceAssemblyGroup)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        await new Test<TAnalyzer, TCodeFixProvider>
        {
            TestCode = originalSource,
            FixedCode = fixedSource,
            ReferenceAssemblies = referenceAssemblies,
        }.RunAsync().ConfigureAwait(false);
    }
}
