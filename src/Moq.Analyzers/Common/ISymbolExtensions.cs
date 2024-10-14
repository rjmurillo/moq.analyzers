namespace Moq.Analyzers.Common;

internal static class ISymbolExtensions
{
    /// <summary>
    /// Determines whether the symbol is an instance of the specified symbol.
    /// </summary>
    /// <typeparam name="TSymbol">The type of the <see cref="ISymbol"/> to compare.</typeparam>
    /// <param name="symbol">The symbol to compare.</param>
    /// <param name="other">The symbol to compare to.</param>
    /// <param name="symbolEqualityComparer">The <see cref="SymbolEqualityComparer"/> to use for equality.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="symbol"/> is an instance of <paramref name="other"/>, either as a direct match,
    /// or as a specialaization; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// As an example, <c>Type.Method&lt;int&gt;()</c> is an instance of <c>Type.Method&lt;T&gt;()</c>.
    /// </remarks>
    public static bool IsInstanceOf<TSymbol>(this ISymbol symbol, TSymbol other, SymbolEqualityComparer? symbolEqualityComparer = null)
        where TSymbol : class, ISymbol
    {
        symbolEqualityComparer ??= SymbolEqualityComparer.Default;

        return symbol switch
        {
            IMethodSymbol methodSymbol => symbolEqualityComparer.Equals(methodSymbol.OriginalDefinition, other),
            _ => symbolEqualityComparer.Equals(symbol, other),
        };
    }

    /// <inheritdoc cref="IsInstanceOf{TSymbol}(ISymbol, TSymbol, SymbolEqualityComparer?)"/>
    /// <param name="symbol">The symbol to compare.</param>
    /// <param name="others">
    /// The symbols to compare to. Returns <see langword="true"/> if <paramref name="symbol"/> matches any of others.
    /// </param>
    /// <param name="symbolEqualityComparer">The <see cref="SymbolEqualityComparer"/> to use for equality.</param>
    public static bool IsInstanceOf<TSymbol>(this ISymbol symbol, IEnumerable<TSymbol> others, SymbolEqualityComparer? symbolEqualityComparer = null)
        where TSymbol : class, ISymbol
    {
        symbolEqualityComparer ??= SymbolEqualityComparer.Default;

        return others.Any(other => symbol.IsInstanceOf(other, symbolEqualityComparer));
    }

    public static bool IsConstructor(this ISymbol symbol)
    {
        return symbol.DeclaredAccessibility != Accessibility.Private
                && symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } and { IsStatic: false };
    }

    public static bool IsMethodReturnTypeTask(this ISymbol methodSymbol)
    {
        string type = methodSymbol.ToDisplayString();
        return string.Equals(type, "System.Threading.Tasks.Task", StringComparison.Ordinal)
               || string.Equals(type, "System.Threading.ValueTask", StringComparison.Ordinal)
               || type.StartsWith("System.Threading.Tasks.Task<", StringComparison.Ordinal)
               || (type.StartsWith("System.Threading.Tasks.ValueTask<", StringComparison.Ordinal)
                   && type.EndsWith(".Result", StringComparison.Ordinal));
    }
}
