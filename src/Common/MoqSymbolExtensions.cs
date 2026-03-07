namespace Moq.Analyzers.Common;

/// <summary>
/// Extension methods for detecting Moq-specific symbols (Setup, Verify, Returns, Callback, Raises, etc.).
/// </summary>
internal static class MoqSymbolExtensions
{
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
    /// <returns>True if the symbol is a Returns method from any Moq.Language.IReturns interface; otherwise false.</returns>
    internal static bool IsMoqReturnsMethod(this ISymbol symbol, MoqKnownSymbols knownSymbols)
    {
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
