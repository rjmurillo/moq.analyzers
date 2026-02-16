using System.Reflection;
using System.Runtime.Loader;

namespace Moq.Analyzers.Test;

public class AnalyzerAssemblyCompatibilityTests
{
    // Primary shipped assemblies built by this project
    private static readonly string PrimaryAnalyzerAssembly = "Moq.Analyzers";
    private static readonly string PrimaryCodeFixAssembly = "Moq.CodeFixes";

    // Bundled third-party assemblies included in the package
    private static readonly string BundledAnalyzerUtilities = "Microsoft.CodeAnalysis.AnalyzerUtilities";

    // Maximum assembly versions that the minimum supported SDK host (.NET 8) provides.
    // The analyzer must not reference anything higher, or it will fail to load with CS8032.
    // See: https://github.com/rjmurillo/moq.analyzers/issues/850
    private static readonly Version MaxImmutableVersion = new(8, 0, 0, 0);
    private static readonly Version MaxMetadataVersion = new(8, 0, 0, 0);

    public static TheoryData<string> ShippedAssemblies =>
        new()
        {
            { PrimaryAnalyzerAssembly },
            { PrimaryCodeFixAssembly },
            { BundledAnalyzerUtilities },
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

            // For bundled third-party DLLs, verify we are testing the same artifact
            // that the primary analyzer assembly references, not a different version
            // that might exist in the test output from the test project's own dependencies.
            if (string.Equals(assemblyName, BundledAnalyzerUtilities, StringComparison.Ordinal))
            {
                VerifyBundledAssemblyMatchesPrimaryReference(context, testAssembly.DirectoryName!, assembly);
            }

            AssemblyName[] references = assembly.GetReferencedAssemblies();

            AssertVersionNotExceeded(assemblyName, references, "System.Collections.Immutable", MaxImmutableVersion);
            AssertVersionNotExceeded(assemblyName, references, "System.Reflection.Metadata", MaxMetadataVersion);
        }
        finally
        {
            context.Unload();
        }
    }

    private static void VerifyBundledAssemblyMatchesPrimaryReference(
        AssemblyLoadContext context,
        string outputDirectory,
        Assembly bundledAssembly)
    {
        // Load the primary analyzer assembly to verify the bundled assembly matches
        // what the analyzer actually references. This ensures we're testing the
        // artifact that will be packaged, not a different version from the test project.
        string primaryPath = Path.Combine(outputDirectory, $"{PrimaryAnalyzerAssembly}.dll");
        Assembly primaryAssembly = context.LoadFromAssemblyPath(primaryPath);

        AssemblyName? expectedReference = primaryAssembly
            .GetReferencedAssemblies()
            .FirstOrDefault(r => string.Equals(r.Name, bundledAssembly.GetName().Name, StringComparison.Ordinal));

        Assert.NotNull(expectedReference);
        Assert.Equal(
            expectedReference.Version,
            bundledAssembly.GetName().Version);
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
