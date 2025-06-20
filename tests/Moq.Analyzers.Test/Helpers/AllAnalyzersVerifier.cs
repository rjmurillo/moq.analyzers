using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// Verifier that tests code against ALL Moq analyzers to ensure no unwanted diagnostics are reported.
/// This is useful for testing that valid patterns don't trigger false positive warnings from any analyzer.
/// </summary>
internal static class AllAnalyzersVerifier
{
    public static async Task VerifyAllAnalyzersAsync(string source, string referenceAssemblyGroup)
    {
        await VerifyAllAnalyzersAsync(source, referenceAssemblyGroup, configFileName: null, configContent: null).ConfigureAwait(false);
    }

    public static async Task VerifyAllAnalyzersAsync(string source, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        // Test each analyzer individually to ensure none report diagnostics
        await AnalyzerVerifier<AsShouldBeUsedOnlyForInterfaceAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<CallbackSignatureShouldMatchMockedMethodAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<ConstructorArgumentsShouldMatchAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<MockGetShouldNotTakeLiteralsAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<NoMethodsInPropertySetupAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<NoSealedClassMocksAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<RaiseEventArgumentsShouldMatchEventSignatureAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<SetExplicitMockBehaviorAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<SetStrictMockBehaviorAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        await AnalyzerVerifier<SetupShouldNotIncludeAsyncResultAnalyzer>.VerifyAnalyzerAsync(source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
    }
}
