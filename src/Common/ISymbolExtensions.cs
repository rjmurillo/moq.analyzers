using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Moq.Analyzers.Common;

/// <summary>
/// General-purpose Roslyn symbol extension methods with no Moq-specific dependencies.
/// </summary>
internal static partial class ISymbolExtensions
{
    internal static bool IsConstructor(this ISymbol symbol)
    {
        return symbol.DeclaredAccessibility != Accessibility.Private
                && symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } and { IsStatic: false };
    }

    /// <summary>
    /// Determines whether the symbol is an instance of the specified symbol.
    /// </summary>
    /// <typeparam name="TSymbol">The type of the <see cref="ISymbol"/> to compare.</typeparam>
    /// <param name="symbol">The symbol to compare.</param>
    /// <param name="other">The symbol to compare to.</param>
    /// <param name="symbolEqualityComparer">The <see cref="SymbolEqualityComparer"/> to use for equality.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="symbol"/> is an instance of <paramref name="other"/>, either as a direct match,
    /// or as a specialization; otherwise, <see langword="false"/>.
    /// </returns>
    /// <example>
    /// <c>MyType.MyMethod{int}()</c> is an instance of <c>MyType.MyMethod{T}()</c>.
    /// </example>
    /// <example>
    /// <c>MyType{int}()</c> is an instance of <c>MyType{T}()</c>.
    /// </example>
    internal static bool IsInstanceOf<TSymbol>(this ISymbol? symbol, TSymbol? other, SymbolEqualityComparer? symbolEqualityComparer = null)
        where TSymbol : class, ISymbol
    {
        symbolEqualityComparer ??= SymbolEqualityComparer.Default;

        if (symbol is IMethodSymbol method)
        {
            if (symbolEqualityComparer.Equals(method.OriginalDefinition, other))
            {
                return true;
            }

            // Reduced extension methods have a different OriginalDefinition than
            // the static method on the containing type. Check the unreduced form.
            return method.ReducedFrom != null
                && symbolEqualityComparer.Equals(method.ReducedFrom.OriginalDefinition, other);
        }

        if (symbol is IParameterSymbol parameter && other is IParameterSymbol otherParameter)
        {
            return parameter.Ordinal == otherParameter.Ordinal
                   && symbolEqualityComparer.Equals(parameter.OriginalDefinition, otherParameter.OriginalDefinition);
        }

        if (symbol is INamedTypeSymbol namedType)
        {
            if (namedType.IsGenericType)
            {
                namedType = namedType.ConstructedFrom;
            }

            return symbolEqualityComparer.Equals(namedType, other);
        }

        return symbolEqualityComparer.Equals(symbol, other);
    }

    /// <inheritdoc cref="IsInstanceOf{TSymbol}(ISymbol, TSymbol, SymbolEqualityComparer?)"/>
    /// <param name="symbol">The symbol to compare.</param>
    /// <param name="others">
    /// The symbols to compare to. Returns <see langword="true"/> if <paramref name="symbol"/> matches any of others.
    /// </param>
    /// <param name="matchingSymbol">
    /// The matching symbol if <paramref name="symbol"/> is an instance of any of <paramref name="others"/>. <see langword="null"/> otherwise.
    /// </param>
    /// <param name="symbolEqualityComparer">The <see cref="SymbolEqualityComparer"/> to use for equality.</param>
    internal static bool IsInstanceOf<TSymbol>(this ISymbol symbol, ImmutableArray<TSymbol> others, [NotNullWhen(true)] out TSymbol? matchingSymbol, SymbolEqualityComparer? symbolEqualityComparer = null)
        where TSymbol : class, ISymbol
    {
        symbolEqualityComparer ??= SymbolEqualityComparer.Default;

        foreach (TSymbol other in others)
        {
            if (symbol.IsInstanceOf(other, symbolEqualityComparer))
            {
                matchingSymbol = other;
                return true;
            }
        }

        matchingSymbol = null;
        return false;
    }

    /// <inheritdoc cref="IsInstanceOf{TSymbol}(ISymbol, TSymbol, SymbolEqualityComparer?)"/>
    /// <param name="symbol">The symbol to compare.</param>
    /// <param name="others">
    /// The symbols to compare to. Returns <see langword="true"/> if <paramref name="symbol"/> matches any of others.
    /// </param>
    /// <param name="symbolEqualityComparer">The <see cref="SymbolEqualityComparer"/> to use for equality.</param>
    internal static bool IsInstanceOf<TSymbol>(this ISymbol symbol, ImmutableArray<TSymbol> others, SymbolEqualityComparer? symbolEqualityComparer = null)
        where TSymbol : class, ISymbol
    {
        return symbol.IsInstanceOf(others, out _, symbolEqualityComparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsOverridable(this ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true } => false,
            _ when symbol.ContainingType?.TypeKind == TypeKind.Interface => true,
            _ => !symbol.IsSealed &&
                  (symbol.IsVirtual || symbol.IsAbstract || symbol is { IsOverride: true, IsSealed: false }),
        };
    }
}
