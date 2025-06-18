namespace Moq.Analyzers.Common;

internal static class ITypeSymbolExtensions
{
    /// <summary>
    /// Get the base types of a type, including the type itself.
    /// </summary>
    /// <remarks>
    /// Use this to walk the inheritance chain of a type.
    /// </remarks>
    /// <param name="type">The <see cref="ITypeSymbol"/> to walk.</param>
    /// <returns>The type and any inherited types.</returns>
    internal static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
    {
        ITypeSymbol? current = type;
        while (current is not null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    /// <summary>
    /// Checks if a type inherits from or implements a given base type.
    /// </summary>
    /// <param name="type">The <see cref="ITypeSymbol"/> to check.</param>
    /// <param name="baseType">The base <see cref="ITypeSymbol"/> to check for.</param>
    /// <returns><see langword="true"/> if the <paramref name="type"/> inherits from or implements the <paramref name="baseType"/>, <see langword="false"/> otherwise.</returns>
    internal static bool InheritsFromOrImplements(this ITypeSymbol? type, ITypeSymbol? baseType)
    {
        while (type != null)
        {
            if (SymbolEqualityComparer.Default.Equals(type, baseType))
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }
}
