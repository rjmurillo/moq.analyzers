using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// The testing framework does heavy work to resolve references for set of <see cref="ReferenceAssemblies"/>, including potentially
/// running the NuGet client to download packages. This class caches the ReferenceAssemblies class (which is thread-safe), so that
/// package resolution only happens once for a given configuration.
/// </summary>
/// <remarks>
/// It would be more straightforward to pass around ReferenceAssemblies instances directly, but using non-primitive types causes
/// Visual Studio's Test Explorer to collapse all test cases down to a single entry, which makes it harder to see which test cases
/// are failing or debug a single test case.
/// </remarks>
internal static class ReferenceAssemblyCatalog
{
    public static string Net80WithOldMoq => nameof(Net80WithOldMoq);

    public static string Net80WithNewMoq => nameof(Net80WithNewMoq);

    public static IReadOnlyDictionary<string, ReferenceAssemblies> Catalog { get; } = new Dictionary<string, ReferenceAssemblies>(StringComparer.Ordinal)
    {
        // 4.8.2 was one of the first popular versions of Moq. Ensure this version is prior to 4.13.1, as it changed the internal
        // implementation of `.As<T>()` (see https://github.com/devlooped/moq/commit/b552aeddd82090ee0f4743a1ab70a16f3e6d2d11).
        { nameof(Net80WithOldMoq), ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Moq", "4.8.2")]) },

        // 4.18.4 is currently the most downloaded version of Moq.
        { nameof(Net80WithNewMoq), ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Moq", "4.18.4")]) },
    };
}
