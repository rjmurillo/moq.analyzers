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

    /// <summary>
    /// Determines whether a type symbol represents a Task or ValueTask type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="knownSymbols">The known symbols from the compilation.</param>
    /// <returns>True if the type is Task, Task&lt;T&gt;, ValueTask, or ValueTask&lt;T&gt;; otherwise false.</returns>
    public static bool IsTaskOrValueTaskType(this ITypeSymbol typeSymbol, MoqKnownSymbols knownSymbols)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Check for Task, Task<T>, ValueTask, or ValueTask<T>
        INamedTypeSymbol originalDefinition = namedType.OriginalDefinition;

        return SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.Task) ||
               SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.Task1) ||
               SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ValueTask) ||
               SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ValueTask1);
    }

    internal static bool IsMoqSetupMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.Mock1Setup) && symbol is IMethodSymbol { IsGenericMethod: true };
    }

    internal static bool IsMoqSetupAddMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.Mock1SetupAdd);
    }

    internal static bool IsMoqSetupRemoveMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.Mock1SetupRemove);
    }

    internal static bool IsMoqEventSetupMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsMoqSetupAddMethod(knownSymbols) || symbol.IsMoqSetupRemoveMethod(knownSymbols);
    }

    internal static bool IsMoqSetupSequenceMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.Mock1SetupSequence) && symbol is IMethodSymbol { IsGenericMethod: true };
    }

    internal static bool IsMoqVerificationMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.Mock1Verify) ||
               symbol.IsInstanceOf(knownSymbols.Mock1VerifyGet) ||
               symbol.IsInstanceOf(knownSymbols.Mock1VerifySet) ||
               symbol.IsInstanceOf(knownSymbols.Mock1VerifyNoOtherCalls);
    }

    /// <summary>
    /// Determines whether a symbol is a Moq Returns method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a Returns method from Moq.Language.IReturns; otherwise false.</returns>
    internal static bool IsMoqReturnsMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        // Check if this method symbol matches any of the known Returns methods
        return symbol.IsInstanceOf(knownSymbols.IReturnsReturns) ||
               symbol.IsInstanceOf(knownSymbols.IReturns1Returns) ||
               symbol.IsInstanceOf(knownSymbols.IReturns2Returns);
    }

    /// <summary>
    /// Determines whether a symbol is a Moq Callback method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a Callback method from Moq.Language.ICallback; otherwise false.</returns>
    internal static bool IsMoqCallbackMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        // Check if this method symbol matches any of the known Callback methods
        return symbol.IsInstanceOf(knownSymbols.ICallbackCallback) ||
               symbol.IsInstanceOf(knownSymbols.ICallback1Callback) ||
               symbol.IsInstanceOf(knownSymbols.ICallback2Callback);
    }

    /// <summary>
    /// Determines whether a symbol is a Moq Raises method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a Raises or RaisesAsync method from Moq.Language interfaces; otherwise false.</returns>
    internal static bool IsMoqRaisesMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        if (symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        // Primary: Use symbol-based detection for known Moq interfaces
        if (IsKnownMoqRaisesMethod(symbol, knownSymbols))
        {
            return true;
        }

        // TODO: Replace this with comprehensive symbol-based detection
        // The current symbol-based detection in IsKnownMoqRaisesMethod covers the standard
        // Moq interfaces (ICallback, IReturns, IRaiseable) but may not cover all possible
        // Raises method scenarios. A complete replacement would require:
        // 1. Analysis of all possible Moq Raises patterns in different versions
        // 2. Enhanced MoqKnownSymbols to include any missing interface patterns
        // 3. Comprehensive test coverage for edge cases
        //
        // For now, keep the conservative string-based fallback to avoid breaking existing functionality
        return IsConservativeRaisesMethodFallback(methodSymbol);
    }

    /// <summary>
    /// Checks if the symbol matches any of the known Moq Raises method symbols.
    /// This method handles all supported Moq interfaces that provide Raises functionality.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol matches a known Moq Raises method; otherwise false.</returns>
    private static bool IsKnownMoqRaisesMethod(ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return IsCallbackRaisesMethod(symbol, knownSymbols) ||
               IsReturnsRaisesMethod(symbol, knownSymbols) ||
               IsRaiseableMethod(symbol, knownSymbols);
    }

    /// <summary>
    /// Checks if the symbol is a Raises method from ICallback interfaces.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a callback Raises method; otherwise false.</returns>
    private static bool IsCallbackRaisesMethod(ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.ICallbackRaises) ||
               symbol.IsInstanceOf(knownSymbols.ICallback1Raises) ||
               symbol.IsInstanceOf(knownSymbols.ICallback2Raises);
    }

    /// <summary>
    /// Checks if the symbol is a Raises method from IReturns interfaces.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a returns Raises method; otherwise false.</returns>
    private static bool IsReturnsRaisesMethod(ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.IReturnsRaises) ||
               symbol.IsInstanceOf(knownSymbols.IReturns1Raises) ||
               symbol.IsInstanceOf(knownSymbols.IReturns2Raises);
    }

    /// <summary>
    /// Checks if the symbol is a Raises method from IRaiseable interfaces.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a raiseable method; otherwise false.</returns>
    private static bool IsRaiseableMethod(ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.IRaiseableRaises) ||
               symbol.IsInstanceOf(knownSymbols.IRaiseableAsyncRaisesAsync);
    }

    /// <summary>
    /// Conservative fallback for Moq Raises method detection using string-based name checking.
    ///
    /// NOTE: This method should eventually be replaced with comprehensive symbol-based detection.
    /// It provides a safety net for Raises method patterns that may not be covered by the current
    /// symbol-based detection in IsKnownMoqRaisesMethod.
    ///
    /// The method is intentionally conservative to minimize false positives while ensuring
    /// that legitimate Moq Raises calls are detected.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check.</param>
    /// <returns>True if this is likely a Moq Raises method; otherwise false.</returns>
    private static bool IsConservativeRaisesMethodFallback(IMethodSymbol methodSymbol)
    {
        // Only match exact "Raises" or "RaisesAsync" method names
        if (!string.Equals(methodSymbol.Name, "Raises", StringComparison.Ordinal) &&
            !string.Equals(methodSymbol.Name, "RaisesAsync", StringComparison.Ordinal))
        {
            return false;
        }

        // Must be in a Moq namespace to reduce false positives
        string? containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
        return containingNamespace?.StartsWith("Moq", StringComparison.Ordinal) == true;
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
