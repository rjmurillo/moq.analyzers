using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

internal static class AnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static async Task VerifyAnalyzerAsync(string source, string referenceAssemblyGroup)
    {
        await VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName: null, configContent: null).ConfigureAwait(false);
    }

    public static async Task VerifyAnalyzerAsync(string source, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        Test<TAnalyzer, EmptyCodeFixProvider> test = new Test<TAnalyzer, EmptyCodeFixProvider>
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = referenceAssemblies,
        };

        if (configFileName != null && configContent != null)
        {
            test.TestState.AnalyzerConfigFiles.Add((configFileName, configContent));
        }

        await test.RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostic results for the given source code.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expected">The expected diagnostic results.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyAnalyzerAsync(string source, DiagnosticResult[] expected, string referenceAssemblyGroup)
    {
        await VerifyAnalyzerAsync(source, expected, referenceAssemblyGroup, configFileName: null, configContent: null).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostic results for the given source code.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expected">The expected diagnostic results.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <param name="configFileName">Optional analyzer config file name.</param>
    /// <param name="configContent">Optional analyzer config file content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyAnalyzerAsync(string source, DiagnosticResult[] expected, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        Test<TAnalyzer, EmptyCodeFixProvider> test = new Test<TAnalyzer, EmptyCodeFixProvider>
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = referenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);

        if (configFileName != null && configContent != null)
        {
            test.TestState.AnalyzerConfigFiles.Add((configFileName, configContent));
        }

        await test.RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostic result for the given source code.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expected">The expected diagnostic result.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyAnalyzerAsync(string source, DiagnosticResult expected, string referenceAssemblyGroup)
    {
        await VerifyAnalyzerAsync(source, [expected], referenceAssemblyGroup).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that the analyzer produces the expected diagnostic result for the given source code.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="expected">The expected diagnostic result.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <param name="configFileName">Optional analyzer config file name.</param>
    /// <param name="configContent">Optional analyzer config file content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyAnalyzerAsync(string source, DiagnosticResult expected, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        await VerifyAnalyzerAsync(source, [expected], referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
    }
}
