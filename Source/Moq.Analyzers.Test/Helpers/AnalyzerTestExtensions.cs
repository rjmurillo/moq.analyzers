using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

internal static class AnalyzerTestExtensions
{
    public static TAnalyzerTest SetDefaults<TAnalyzerTest, TVerifier>(this TAnalyzerTest test)
        where TAnalyzerTest : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        test.ReferenceAssemblies = ReferenceAssemblyCatalog.Net80WithOldMoq;

        return test;
    }
}
