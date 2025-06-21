using System.Reflection;

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// Verifier that tests code against ALL Moq analyzers to ensure no unwanted diagnostics are reported.
/// This is useful for testing that valid patterns don't trigger false positive warnings from any analyzer.
/// Uses reflection to dynamically discover all analyzer types to avoid manual maintenance.
///
/// <para>
/// The verifier automatically discovers all concrete types in the Moq.Analyzers namespace that:
/// - Inherit from DiagnosticAnalyzer
/// - Are not abstract
/// - Have the [DiagnosticAnalyzer] attribute.
/// </para>
///
/// <para>
/// When new analyzers are added to the codebase, they are automatically included in testing
/// without requiring any changes to this file or manual maintenance of analyzer lists.
/// </para>
/// </summary>
internal static class AllAnalyzersVerifier
{
    private static readonly Lazy<ImmutableArray<Type>> AllAnalyzerTypes = new(DiscoverAnalyzerTypes);

    internal static async Task VerifyAllAnalyzersAsync(string source, string referenceAssemblyGroup)
    {
        await VerifyAllAnalyzersAsync(source, referenceAssemblyGroup, configFileName: null, configContent: null).ConfigureAwait(false);
    }

    internal static async Task VerifyAllAnalyzersAsync(string source, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        // Dynamically test each analyzer to ensure none report diagnostics
        foreach (Type analyzerType in AllAnalyzerTypes.Value)
        {
            await VerifyAnalyzerDynamicallyAsync(analyzerType, source, referenceAssemblyGroup, configFileName, configContent).ConfigureAwait(false);
        }
    }

    private static ImmutableArray<Type> DiscoverAnalyzerTypes()
    {
        // Get the assembly containing the analyzers
        Assembly analyzerAssembly = typeof(AsShouldBeUsedOnlyForInterfaceAnalyzer).Assembly;

        // Find all concrete DiagnosticAnalyzer types in the Moq.Analyzers namespace
        return analyzerAssembly.GetTypes()
            .Where(type => string.Equals(type.Namespace, "Moq.Analyzers", StringComparison.Ordinal) &&
                !type.IsAbstract &&
                typeof(DiagnosticAnalyzer).IsAssignableFrom(type) &&
                type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() != null)
            .OrderBy(type => type.Name, StringComparer.Ordinal) // For deterministic ordering
            .ToImmutableArray();
    }

    private static async Task VerifyAnalyzerDynamicallyAsync(Type analyzerType, string source, string referenceAssemblyGroup, string? configFileName, string? configContent)
    {
        // Create AnalyzerVerifier<TAnalyzer> using reflection
        Type analyzerVerifierType = typeof(AnalyzerVerifier<>).MakeGenericType(analyzerType);

        // Get the VerifyAnalyzerAsync method
        MethodInfo? verifyMethod = analyzerVerifierType.GetMethod(
            nameof(AnalyzerVerifier<AsShouldBeUsedOnlyForInterfaceAnalyzer>.VerifyAnalyzerAsync),
            BindingFlags.Static | BindingFlags.Public,
            new[] { typeof(string), typeof(string), typeof(string), typeof(string) });

        if (verifyMethod == null)
        {
            throw new InvalidOperationException($"Could not find VerifyAnalyzerAsync method for analyzer type {analyzerType.Name}");
        }

        // Invoke the method
        object? task = verifyMethod.Invoke(null, new object?[] { source, referenceAssemblyGroup, configFileName, configContent });
        if (task is Task taskResult)
        {
            await taskResult.ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException($"{nameof(VerifyAllAnalyzersAsync)} did not return a Task for analyzer type {analyzerType.Name}");
        }
    }
}
