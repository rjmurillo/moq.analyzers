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
    /// Determines whether a symbol is a Moq Throws method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a Throws method from Moq.Language.IThrows; otherwise false.</returns>
    internal static bool IsMoqThrowsMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.IThrowsThrows);
    }

    /// <summary>
    /// Determines whether a symbol is a Moq ReturnsAsync extension method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a ReturnsAsync method from Moq.ReturnsExtensions; otherwise false.</returns>
    internal static bool IsMoqReturnsAsyncMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.ReturnsExtensionsReturnsAsync);
    }

    /// <summary>
    /// Determines whether a symbol is a Moq ThrowsAsync extension method.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a ThrowsAsync method from Moq.ReturnsExtensions; otherwise false.</returns>
    internal static bool IsMoqThrowsAsyncMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.ReturnsExtensionsThrowsAsync);
    }

    /// <summary>
    /// Determines whether a symbol is any Moq method that specifies a return value
    /// (Returns, ReturnsAsync, Throws, or ThrowsAsync).
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="knownSymbols">The known symbols for type checking.</param>
    /// <returns>True if the symbol is a return value specification method; otherwise false.</returns>
    internal static bool IsMoqReturnValueSpecificationMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsMoqReturnsMethod(knownSymbols) ||
               symbol.IsMoqThrowsMethod(knownSymbols) ||
               symbol.IsMoqReturnsAsyncMethod(knownSymbols) ||
               symbol.IsMoqThrowsAsyncMethod(knownSymbols);
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
        if (symbol is not IMethodSymbol)
        {
            return false;
        }

        return IsCallbackRaisesMethod(symbol, knownSymbols) ||
            IsReturnsRaisesMethod(symbol, knownSymbols) ||
            IsRaiseableMethod(symbol, knownSymbols) ||
            IsSetupRaisesMethod(symbol, knownSymbols) ||
            IsConcreteSetupPhraseRaisesMethod(symbol, knownSymbols);
    }

    /// <summary>
    /// Checks if the symbol is a Raises method from ISetup / ISetupPhrase interfaces.
    /// </summary>
    private static bool IsSetupRaisesMethod(ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.ISetup1Raises) ||
            symbol.IsInstanceOf(knownSymbols.ISetupPhrase1Raises) ||
            symbol.IsInstanceOf(knownSymbols.ISetupGetter1Raises) ||
            symbol.IsInstanceOf(knownSymbols.ISetupSetter1Raises);
    }

    /// <summary>
    /// Checks if the symbol is a Raises method on concrete setup phrase types (Void/NonVoid).
    /// </summary>
    private static bool IsConcreteSetupPhraseRaisesMethod(ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
        return symbol.IsInstanceOf(knownSymbols.VoidSetupPhrase1Raises) ||
               symbol.IsInstanceOf(knownSymbols.NonVoidSetupPhrase2Raises);
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
               symbol.IsInstanceOf(knownSymbols.ICallback2Raises) ||
               symbol.IsInstanceOf(knownSymbols.ISetupGetterRaises) ||
               symbol.IsInstanceOf(knownSymbols.ISetupSetterRaises);
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
               symbol.IsInstanceOf(knownSymbols.IRaiseableAsyncRaisesAsync) ||
               symbol.IsInstanceOf(knownSymbols.IRaise1Raises) ||
               symbol.IsInstanceOf(knownSymbols.IRaise1RaisesAsync);
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
