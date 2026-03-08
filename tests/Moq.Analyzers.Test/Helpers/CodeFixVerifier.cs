using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

internal static class CodeFixVerifier<TAnalyzer, TCodeFixProvider>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixProvider : CodeFixProvider, new()
{
    public static async Task VerifyCodeFixAsync(string originalSource, string fixedSource, string referenceAssemblyGroup, int? numberOfIncrementalIterations = null, int? numberOfFixAllIterations = null, CompilerDiagnostics? compilerDiagnostics = null)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        Test<TAnalyzer, TCodeFixProvider> test = new Test<TAnalyzer, TCodeFixProvider>
        {
            TestCode = originalSource,
            FixedCode = fixedSource,
            ReferenceAssemblies = referenceAssemblies,
        };

        if (numberOfIncrementalIterations.HasValue)
        {
            test.NumberOfIncrementalIterations = numberOfIncrementalIterations.Value;
        }

        if (numberOfFixAllIterations.HasValue)
        {
            test.NumberOfFixAllIterations = numberOfFixAllIterations.Value;
        }

        if (compilerDiagnostics.HasValue)
        {
            test.CompilerDiagnostics = compilerDiagnostics.Value;
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
