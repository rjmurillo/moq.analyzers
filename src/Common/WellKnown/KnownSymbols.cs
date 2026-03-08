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
    internal KnownSymbols(WellKnownTypeProvider typeProvider)
    {
        if (typeProvider is null)
        {
            throw new ArgumentNullException(nameof(typeProvider));
        }

        TypeProvider = typeProvider;
    }

    internal KnownSymbols(Compilation compilation)
        : this(WellKnownTypeProvider.GetOrCreate(compilation))
    {
    }

    /// <summary>
    /// Gets the class <see cref="System.Threading.Tasks.Task"/>.
    /// </summary>
    internal INamedTypeSymbol? Task => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.Task");

    /// <summary>
    /// Gets the class <see cref="System.Threading.Tasks.Task{T}"/>.
    /// </summary>
    internal INamedTypeSymbol? Task1 => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.Task`1");

    /// <summary>
    /// Gets the class <see cref="System.Threading.Tasks.ValueTask"/>.
    /// </summary>
    internal INamedTypeSymbol? ValueTask => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.ValueTask");

    /// <summary>
    /// Gets the class <see cref="System.EventHandler{TEventArgs}"/>.
    /// </summary>
    internal INamedTypeSymbol? EventHandler1 => TypeProvider.GetOrCreateTypeByMetadataName("System.EventHandler`1");

    /// <summary>
    /// Gets the class <see cref="System.Threading.Tasks.ValueTask{T}"/>.
    /// </summary>
    internal INamedTypeSymbol? ValueTask1 => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

    /// <summary>
    /// Gets the class <see cref="System.Action"/>.
    /// </summary>
    internal INamedTypeSymbol? Action0 => TypeProvider.GetOrCreateTypeByMetadataName("System.Action");

    /// <summary>
    /// Gets the class <see cref="System.Action{T}"/>.
    /// </summary>
    internal INamedTypeSymbol? Action1 => TypeProvider.GetOrCreateTypeByMetadataName("System.Action`1");

    /// <summary>
    /// Gets the class <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/>.
    /// </summary>
    internal INamedTypeSymbol? InternalsVisibleToAttribute => TypeProvider.GetOrCreateTypeByMetadataName("System.Runtime.CompilerServices.InternalsVisibleToAttribute");

    protected WellKnownTypeProvider TypeProvider { get; }
}
