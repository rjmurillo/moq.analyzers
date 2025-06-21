namespace Moq.Analyzers.Common;

/// <summary>
/// Provides extension methods for <see cref="INamedTypeSymbol"/> to assist with type analysis in analyzers.
/// </summary>
public static class NamedTypeSymbolExtensions
{
    /// <summary>
    /// Determines if the given <paramref name="namedType"/> represents the generic <see cref="System.EventHandler{TEventArgs}"/> type.
    /// </summary>
    /// <param name="namedType">The symbol to check.</param>
    /// <param name="knownSymbols">A <see cref="KnownSymbols"/> instance providing access to well-known Roslyn symbols.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="namedType"/> is constructed from <see cref="System.EventHandler{TEventArgs}"/> and has exactly one type argument; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method uses <see cref="SymbolEqualityComparer.Default"/> to compare the constructed generic type definition
    /// of <paramref name="namedType"/> with the canonical <see cref="System.EventHandler{TEventArgs}"/> symbol.
    /// </remarks>
    internal static bool IsEventHandlerDelegate(this INamedTypeSymbol namedType, KnownSymbols knownSymbols)
    {
        return knownSymbols.EventHandler1 is not null
            && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, knownSymbols.EventHandler1)
            && namedType.TypeArguments.Length == 1;
    }

    /// <summary>
    /// Determines if the given <paramref name="namedType"/> represents <see cref="System.Action"/> or <see cref="System.Action{T}"/>.
    /// </summary>
    /// <param name="namedType">The symbol to check.</param>
    /// <param name="knownSymbols">A <see cref="KnownSymbols"/> instance providing access to well-known Roslyn symbols.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="namedType"/> is constructed from <see cref="System.Action"/> or <see cref="System.Action{T}"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method uses <see cref="SymbolEqualityComparer.Default"/> to compare the constructed generic type definition
    /// of <paramref name="namedType"/> with the canonical <see cref="System.Action"/> and <see cref="System.Action{T}"/> symbols.
    /// </remarks>
    internal static bool IsActionDelegate(this INamedTypeSymbol namedType, KnownSymbols knownSymbols)
    {
        return (knownSymbols.Action0 is not null && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, knownSymbols.Action0))
            || (knownSymbols.Action1 is not null && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, knownSymbols.Action1));
    }
}
