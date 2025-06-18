using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Moq.Analyzers.Common;

internal static class ISymbolExtensions
{
    public static bool IsConstructor(this ISymbol symbol)
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
    public static bool IsInstanceOf<TSymbol>(this ISymbol? symbol, TSymbol? other, SymbolEqualityComparer? symbolEqualityComparer = null)
        where TSymbol : class, ISymbol
    {
        symbolEqualityComparer ??= SymbolEqualityComparer.Default;

        if (symbol is IMethodSymbol method)
        {
            return symbolEqualityComparer.Equals(method.OriginalDefinition, other);
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
    public static bool IsInstanceOf<TSymbol>(this ISymbol symbol, ImmutableArray<TSymbol> others, [NotNullWhen(true)] out TSymbol? matchingSymbol, SymbolEqualityComparer? symbolEqualityComparer = null)
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
    public static bool IsInstanceOf<TSymbol>(this ISymbol symbol, ImmutableArray<TSymbol> others, SymbolEqualityComparer? symbolEqualityComparer = null)
        where TSymbol : class, ISymbol
    {
        return symbol.IsInstanceOf(others, out _, symbolEqualityComparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOverridable(this ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true } => false,
            _ when symbol.ContainingType?.TypeKind == TypeKind.Interface => true,
            _ => !symbol.IsSealed &&
                  (symbol.IsVirtual || symbol.IsAbstract || symbol is { IsOverride: true, IsSealed: false }),
        };
    }

    public static bool IsTaskOrValueResultProperty(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        if (symbol is IPropertySymbol propertySymbol)
        {
            return IsGenericResultProperty(propertySymbol, knownSymbols.Task1)
                   || IsGenericResultProperty(propertySymbol, knownSymbols.ValueTask1);
        }

        return false;
    }

    internal static bool IsMoqSetupMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.Mock1Setup) && symbol is IMethodSymbol { IsGenericMethod: true };
    }

    internal static bool IsMoqVerificationMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.Mock1Verify) ||
               symbol.IsInstanceOf(knownSymbols.Mock1VerifyGet) ||
               symbol.IsInstanceOf(knownSymbols.Mock1VerifySet) ||
               symbol.IsInstanceOf(knownSymbols.Mock1VerifyNoOtherCalls);
    }

    /// <summary>
    /// Checks if a property is the 'Result' property on <see cref="Task{TResult}"/> or <see cref="ValueTask{TResult}"/>.
    /// </summary>
    private static bool IsGenericResultProperty(this ISymbol symbol, INamedTypeSymbol? genericType)
    {
        if (symbol is IPropertySymbol propertySymbol)
        {
            // Check if the property is named "Result"
            if (!string.Equals(propertySymbol.Name, "Result", StringComparison.Ordinal))
            {
                return false;
            }

            return genericType != null &&

                   // If Task<T> type cannot be found, we skip it
                   SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingType.OriginalDefinition, genericType);
        }

        return false;
    }
}
