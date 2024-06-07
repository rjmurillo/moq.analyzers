using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// The testing framework does heavy work to resolve references for set of <see cref="ReferenceAssemblies"/>, including potentially
/// running the NuGet client to download packages. This class caches the ReferenceAssemblies class (which is thread-safe), so that
/// package resolution only happens once for a given configuration.
/// </summary>
/// <remarks>
/// This class is currently very simple and assumes that the only package that will be resolved is Moq for .NET 8.0. As our testing needs
/// get more complicated, we can either manage the combinations ourselves
/// (as done in https://github.com/dotnet/roslyn-analyzers/blob/4d5fd9da36d64d4c3370b8813122e226844fc6ed/src/Test.Utilities/AdditionalMetadataReferences.cs)
/// or consider filing an issue in https://github.com/dotnet/roslyn-sdk to clarify best practices.
/// </remarks>
internal static class ReferenceAssemblyCatalog
{
    // TODO: We should also be testing a newer version of Moq. See https://github.com/rjmurillo/moq.analyzers/issues/58.
    public static ReferenceAssemblies Net80WithOldMoq { get; } = ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Moq", "4.8.2")]);
}
