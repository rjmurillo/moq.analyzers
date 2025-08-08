using Microsoft.CodeAnalysis.CodeFixes;
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

    /// <summary>
    /// Verifies that the code fix correctly handles the specified diagnostic and transforms the source code as expected.
    /// </summary>
    /// <param name="originalSource">The original source code before the fix.</param>
    /// <param name="fixedSource">The expected source code after the fix.</param>
    /// <param name="expected">The expected diagnostic results before the fix is applied.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyCodeFixAsync(string originalSource, string fixedSource, DiagnosticResult[] expected, string referenceAssemblyGroup)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        Test<TAnalyzer, TCodeFixProvider> test = new Test<TAnalyzer, TCodeFixProvider>
        {
            TestCode = originalSource,
            FixedCode = fixedSource,
            ReferenceAssemblies = referenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that the code fix correctly handles the specified diagnostic and transforms the source code as expected.
    /// </summary>
    /// <param name="originalSource">The original source code before the fix.</param>
    /// <param name="fixedSource">The expected source code after the fix.</param>
    /// <param name="expected">The expected diagnostic result before the fix is applied.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyCodeFixAsync(string originalSource, string fixedSource, DiagnosticResult expected, string referenceAssemblyGroup)
    {
        await VerifyCodeFixAsync(originalSource, fixedSource, [expected], referenceAssemblyGroup).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that the code fix correctly handles the specified diagnostics and transforms the source code as expected.
    /// </summary>
    /// <param name="originalSource">The original source code before the fix.</param>
    /// <param name="fixedSource">The expected source code after the fix.</param>
    /// <param name="expected">The expected diagnostic results before the fix is applied.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <param name="configFileName">Optional analyzer config file name.</param>
    /// <param name="configContent">Optional analyzer config file content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyCodeFixAsync(string originalSource, string fixedSource, DiagnosticResult[] expected, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[referenceAssemblyGroup];

        Test<TAnalyzer, TCodeFixProvider> test = new Test<TAnalyzer, TCodeFixProvider>
        {
            TestCode = originalSource,
            FixedCode = fixedSource,
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
    /// Verifies that the code fix correctly handles the specified diagnostic and transforms the source code as expected.
    /// </summary>
    /// <param name="originalSource">The original source code before the fix.</param>
    /// <param name="fixedSource">The expected source code after the fix.</param>
    /// <param name="expected">The expected diagnostic result before the fix is applied.</param>
    /// <param name="referenceAssemblyGroup">The reference assembly group to use.</param>
    /// <param name="configFileName">Optional analyzer config file name.</param>
    /// <param name="configContent">Optional analyzer config file content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task VerifyCodeFixAsync(string originalSource, string fixedSource, DiagnosticResult expected, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        await VerifyCodeFixAsync(originalSource, fixedSource, [expected], referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
    }
}
