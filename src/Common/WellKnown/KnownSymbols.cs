﻿using Analyzer.Utilities;

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
    /// Gets the class <see cref="System.Threading.Tasks.Task{T}"/>.
    /// </summary>
    public INamedTypeSymbol? Task1 => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.Task`1");

    /// <summary>
    /// Gets the class <see cref="System.Threading.Tasks.ValueTask{T}"/>.
    /// </summary>
    public INamedTypeSymbol? ValueTask1 => TypeProvider.GetOrCreateTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

    protected WellKnownTypeProvider TypeProvider { get; }
}
