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

        // Enhanced symbol-based detection for other Moq patterns
        if (IsAdditionalMoqRaisesMethod(methodSymbol, knownSymbols))
        {
            return true;
        }

        // This fallback will be removed in a future version once symbol-based detection is comprehensive
        return false;
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
    /// Enhanced symbol-based detection for additional Moq Raises method patterns.
    /// This method detects Raises methods that may not be directly covered by the standard interfaces.
    /// </summary>
    /// <param name="methodSymbol">The method symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if this is an additional Moq Raises method pattern; otherwise false.</returns>
    private static bool IsAdditionalMoqRaisesMethod(IMethodSymbol methodSymbol, MoqKnownSymbols knownSymbols)
    {
        // Method must be named "Raises" or "RaisesAsync"
        if (!string.Equals(methodSymbol.Name, "Raises", StringComparison.Ordinal) &&
            !string.Equals(methodSymbol.Name, "RaisesAsync", StringComparison.Ordinal))
        {
            return false;
        }

        // Method must be in a Moq namespace to ensure it's a Moq method
        string? namespaceName = methodSymbol.ContainingNamespace?.ToDisplayString();
        if (namespaceName == null || !namespaceName.StartsWith("Moq", StringComparison.Ordinal))
        {
            return false;
        }

        // Additional validation: ensure the method is from a type that implements Moq interfaces
        INamedTypeSymbol? containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return false;
        }

        // Check if the containing type or its interfaces are related to Moq
        return IsTypeRelatedToMoq(containingType, knownSymbols);
    }

    /// <summary>
    /// Determines if a type is related to Moq by checking its interfaces and base types.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the type is related to Moq; otherwise false.</returns>
    private static bool IsTypeRelatedToMoq(INamedTypeSymbol type, MoqKnownSymbols knownSymbols)
    {
        // Check if the type directly implements any known Moq interfaces
        foreach (INamedTypeSymbol implementedInterface in type.AllInterfaces)
        {
            if (IsMoqInterface(implementedInterface, knownSymbols))
            {
                return true;
            }
        }

        // Check base types
        INamedTypeSymbol? current = type.BaseType;
        while (current != null)
        {
            if (IsMoqInterface(current, knownSymbols))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Determines if a type is a known Moq interface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the type is a Moq interface; otherwise false.</returns>
    private static bool IsMoqInterface(INamedTypeSymbol type, MoqKnownSymbols knownSymbols)
    {
        return SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.ICallback) ||
               SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.ICallback1) ||
               SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.ICallback2) ||
               SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.IReturns) ||
               SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.IReturns1) ||
               SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.IReturns2) ||
               SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.IRaiseable) ||
               SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, knownSymbols.IRaiseableAsync);
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
