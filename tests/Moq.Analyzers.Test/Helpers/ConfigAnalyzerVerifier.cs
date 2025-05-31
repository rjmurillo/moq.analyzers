using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// A specialized analyzer verifier for testing scenarios where analyzers are disabled
/// via configuration files like .editorconfig. This helper encapsulates the complexity
/// of setting up Test&lt;&gt; instances with configuration files.
/// </summary>
internal static class ConfigAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static async Task VerifyAnalyzerAsync(string source, string referenceAssemblyGroup, string configFileName, string configContent)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        var test = new Test<TAnalyzer, EmptyCodeFixProvider>
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = referenceAssemblies,
        };

        test.TestState.AnalyzerConfigFiles.Add((configFileName, configContent));

        await test.RunAsync().ConfigureAwait(false);
    }
}