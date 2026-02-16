using System.Reflection;
using System.Runtime.Loader;

namespace Moq.Analyzers.Test;

public class AnalyzerAssemblyCompatibilityTests
{
    // Maximum assembly versions that the minimum supported SDK host (.NET 8) provides.
    // The analyzer must not reference anything higher, or it will fail to load with CS8032.
    // See: https://github.com/rjmurillo/moq.analyzers/issues/850
    private static readonly Version MaxImmutableVersion = new(8, 0, 0, 0);
    private static readonly Version MaxMetadataVersion = new(8, 0, 0, 0);

    public static TheoryData<string> ShippedAssemblies =>
        new()
        {
            { "Moq.Analyzers" },
            { "Moq.CodeFixes" },
            { "Microsoft.CodeAnalysis.AnalyzerUtilities" },
        };

    [Theory]
    [MemberData(nameof(ShippedAssemblies))]
    public void ShippedDlls_MustNotExceedMinimumHostAssemblyVersions(string assemblyName)
    {
        FileInfo testAssembly = new(Assembly.GetExecutingAssembly().Location);
        FileInfo dllFile = new(Path.Combine(testAssembly.DirectoryName!, $"{assemblyName}.dll"));

        Assert.True(dllFile.Exists, $"Expected shipped DLL not found: {dllFile.FullName}");

        AssemblyLoadContext context = new("compat-check", isCollectible: true);
        try
        {
            Assembly assembly = context.LoadFromAssemblyPath(dllFile.FullName);
            AssemblyName[] references = assembly.GetReferencedAssemblies();

            AssertVersionNotExceeded(assemblyName, references, "System.Collections.Immutable", MaxImmutableVersion);
            AssertVersionNotExceeded(assemblyName, references, "System.Reflection.Metadata", MaxMetadataVersion);
        }
        finally
        {
            context.Unload();
        }
    }

    private static void AssertVersionNotExceeded(
        string assemblyName,
        AssemblyName[] references,
        string referenceName,
        Version maxVersion)
    {
        AssemblyName? reference = references.FirstOrDefault(
            r => string.Equals(r.Name, referenceName, StringComparison.Ordinal));

        if (reference?.Version is null)
        {
            return;
        }

        Assert.True(
            reference.Version <= maxVersion,
            $"{assemblyName} references {referenceName} version {reference.Version}, but the minimum supported SDK host only provides {maxVersion}. See: https://github.com/rjmurillo/moq.analyzers/issues/850");
    }
}
