using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers;

namespace Moq.Analyzers.CSharp14.Test;

internal static class CSharp14AllAnalyzersVerifier
{
    private static readonly Lazy<ImmutableArray<Type>> AllAnalyzerTypes = new(DiscoverAnalyzerTypes);

    public static async Task VerifyAllAnalyzersAsync(string source)
    {
        foreach (Type analyzerType in AllAnalyzerTypes.Value)
        {
            await VerifyAnalyzerDynamicallyAsync(analyzerType, source).ConfigureAwait(false);
        }
    }

    private static ImmutableArray<Type> DiscoverAnalyzerTypes()
    {
        Assembly analyzerAssembly = typeof(AsShouldBeUsedOnlyForInterfaceAnalyzer).Assembly;

        return analyzerAssembly.GetTypes()
            .Where(type => string.Equals(type.Namespace, "Moq.Analyzers", StringComparison.Ordinal) &&
                !type.IsAbstract &&
                typeof(DiagnosticAnalyzer).IsAssignableFrom(type) &&
                type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() != null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static async Task VerifyAnalyzerDynamicallyAsync(Type analyzerType, string source)
    {
        Type analyzerVerifierType = typeof(CSharp14AnalyzerVerifier<>).MakeGenericType(analyzerType);

        MethodInfo? verifyMethod = analyzerVerifierType.GetMethod(
            nameof(CSharp14AnalyzerVerifier<AsShouldBeUsedOnlyForInterfaceAnalyzer>.VerifyAnalyzerAsync),
            BindingFlags.Static | BindingFlags.Public,
            new[] { typeof(string) });

        if (verifyMethod == null)
        {
            throw new InvalidOperationException($"Could not find VerifyAnalyzerAsync method for analyzer type {analyzerType.Name}");
        }

        object? task = verifyMethod.Invoke(null, [source]);
        if (task is Task taskResult)
        {
            await taskResult.ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"{nameof(VerifyAllAnalyzersAsync)} did not return a Task for analyzer type {analyzerType.Name}");
    }
}
