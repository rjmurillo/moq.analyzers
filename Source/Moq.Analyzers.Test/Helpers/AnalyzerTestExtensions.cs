using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

internal static class AnalyzerTestExtensions
{
    public static TAnalyzerTest SetDefaults<TAnalyzerTest, TVerifier>(this TAnalyzerTest test)
        where TAnalyzerTest : AnalyzerTest<TVerifier>
        where TVerifier : IVerifier, new()
    {
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Moq", "4.8.2")]); // TODO: See https://github.com/rjmurillo/moq.analyzers/issues/58

        return test;
    }
}
