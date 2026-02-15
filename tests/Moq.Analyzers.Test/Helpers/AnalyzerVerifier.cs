using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

internal static class AnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static async Task VerifyAnalyzerAsync(string source, string referenceAssemblyGroup)
    {
        await VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName: null, configContent: null).ConfigureAwait(false);
    }

    public static async Task VerifyAnalyzerAsync(
        string source,
        string referenceAssemblyGroup,
        string? configFileName,
        string? configContent,
        CompilerDiagnostics? compilerDiagnostics = null)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        Test<TAnalyzer, EmptyCodeFixProvider> test = new Test<TAnalyzer, EmptyCodeFixProvider>
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = referenceAssemblies,
        };

        if (compilerDiagnostics.HasValue)
        {
            test.CompilerDiagnostics = compilerDiagnostics.Value;
        }

        if (configFileName != null && configContent != null)
        {
            test.TestState.AnalyzerConfigFiles.Add((configFileName, configContent));
        }

        await test.RunAsync().ConfigureAwait(false);
    }

    public static async Task VerifyAnalyzerAsync(string source, string referenceAssemblyGroup, CompilerDiagnostics compilerDiagnostics)
    {
        await VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName: null, configContent: null, compilerDiagnostics).ConfigureAwait(false);
    }
}
