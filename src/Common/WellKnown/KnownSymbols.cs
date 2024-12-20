using Analyzer.Utilities;

namespace Moq.Analyzers.Common.WellKnown;

/// <summary>
/// Main entrypoint to access well-known symbols for the analyzer.
/// This class handles caching to prevent multiple lookups for the same symbol.
///
/// It returns a type derived from <see cref="ISymbol"/> in all cases. Use the
/// <seealso cref="ISymbol.ToDisplayString(SymbolDisplayFormat?)"/> when necessary
/// for comparisons with <seealso cref="SyntaxNode"/>s.
/// </summary>
internal class KnownSymbols
{
    public KnownSymbols(WellKnownTypeProvider typeProvider)
    {
        TypeProvider = typeProvider;
    }

    public KnownSymbols(Compilation compilation)
        : this(WellKnownTypeProvider.GetOrCreate(compilation))
    {
    }

    /// <summary>
    /// Gets the class <c>System.Threading.Tasks.Task</c>.
    /// </summary>
    public INamedTypeSymbol? Task => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.Task");

    /// <summary>
    /// Gets the class <c>System.Threading.Tasks.Task&lt;T&gt;</c>.
    /// </summary>
    public INamedTypeSymbol? Task1 => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.Task`1");

    /// <summary>
    /// Gets the class <c>System.Threading.Tasks.ValueTask</c>.
    /// </summary>
    public INamedTypeSymbol? ValueTask => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.ValueTask");

    /// <summary>
    /// Gets the class <c>System.Threading.Tasks.ValueTask&lt;T&gt;</c>.
    /// </summary>
    public INamedTypeSymbol? ValueTask1 => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

    protected WellKnownTypeProvider TypeProvider { get; }
}
