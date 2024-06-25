using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

internal static class CompilationExtensions
{
    /// <summary>
    /// An extension method that performs <see cref="Compilation.GetTypeByMetadataName(string)"/> for multiple metadata names.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to inspect.</param>
    /// <param name="metadataNames">A list of type names to query.</param>
    /// <returns><see langword="null"/> if the type can't be found or there was an ambiguity during lookup.</returns>
    public static ImmutableArray<INamedTypeSymbol> GetTypesByMetadataNames(this Compilation compilation, ReadOnlySpan<string> metadataNames)
    {
        ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(metadataNames.Length);

        foreach (string metadataName in metadataNames)
        {
            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(metadataName);
            if (type is not null)
            {
                builder.Add(type);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Get the Moq.Mock and Moq.Mock`1 type symbols (if part of the compilation).
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to inspect.</param>
    /// <returns>
    /// <see cref="INamedTypeSymbol"/>s for the Moq.Mock symbols that are part of the compilation.
    /// An empty array if none (never <see langword="null"/>).
    /// </returns>
    public static ImmutableArray<INamedTypeSymbol> GetMoqMock(this Compilation compilation)
    {
        return compilation.GetTypesByMetadataNames([WellKnownTypeNames.MoqMock, WellKnownTypeNames.MoqMock1]);
    }
}
