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
public static class ReferenceAssemblyCatalog
{
    /// <summary>
    /// Gets the name of the reference assembly group for .NET 10.0 without Moq.
    /// Used to test analyzer behavior when Moq is not referenced.
    /// </summary>
    public static string Net100 => nameof(Net100);

    /// <summary>
    /// Gets the name of the reference assembly group for .NET 10.0 with an older version of Moq (4.8.2).
    /// </summary>
    public static string Net100WithOldMoq => nameof(Net100WithOldMoq);

    /// <summary>
    /// Gets the name of the reference assembly group for .NET 10.0 with a newer version of Moq (4.18.4).
    /// </summary>
    public static string Net100WithNewMoq => nameof(Net100WithNewMoq);

    /// <summary>
    /// Gets the name of the reference assembly group for .NET 10.0 with Moq (4.18.4) and Microsoft.Extensions.Logging.Abstractions.
    /// </summary>
    public static string Net100WithNewMoqAndLogging => nameof(Net100WithNewMoqAndLogging);

    /// <summary>
    /// Gets the catalog of reference assemblies.
    /// </summary>
    /// <remarks>
    /// The key is the name of the reference assembly group (<see cref="Net100"/>, <see cref="Net100WithOldMoq"/>,
    /// <see cref="Net100WithNewMoq"/>, and <see cref="Net100WithNewMoqAndLogging"/>).
    /// </remarks>
    public static IReadOnlyDictionary<string, ReferenceAssemblies> Catalog { get; } = new Dictionary<string, ReferenceAssemblies>(StringComparer.Ordinal)
    {
        // .NET 10.0 without Moq, used to verify analyzers short-circuit when Moq is not referenced.
        { nameof(Net100), CreateNet100() },

        // 4.8.2 was one of the first popular versions of Moq. Ensure this version is prior to 4.13.1, as it changed the internal
        // implementation of `.As<T>()` (see https://github.com/devlooped/moq/commit/b552aeddd82090ee0f4743a1ab70a16f3e6d2d11).
        { nameof(Net100WithOldMoq), CreateNet100().AddPackages([new PackageIdentity("Moq", "4.8.2")]) },

        // This must be 4.12.0 or later in order to have the new `Mock.Of<T>(MockBehavior)` method (see https://github.com/devlooped/moq/commit/1561c006c87a0894c5257a1e541da44e40e33dd3).
        // 4.18.4 is currently the most downloaded version of Moq.
        { nameof(Net100WithNewMoq), CreateNet100().AddPackages([new PackageIdentity("Moq", "4.18.4")]) },

        // Moq 4.18.4 with Microsoft.Extensions.Logging.Abstractions for ILogger-related analyzer tests.
        {
            nameof(Net100WithNewMoqAndLogging),
            CreateNet100().AddPackages(
            [
                new PackageIdentity("Moq", "4.18.4"),
                new PackageIdentity("Microsoft.Extensions.Logging.Abstractions", "8.0.0"),
            ])
        },
    };

    // Microsoft.CodeAnalysis.Analyzer.Testing 1.1.2-beta1 has no ReferenceAssemblies.Net.Net100, so build it from the ref pack.
    // Replace this with ReferenceAssemblies.Net.Net100 when the testing package ships it.
    private static ReferenceAssemblies CreateNet100()
    {
        return new ReferenceAssemblies(
            "net10.0",
            new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0"),
            Path.Combine("ref", "net10.0"));
    }
}
