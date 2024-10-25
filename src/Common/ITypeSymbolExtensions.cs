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
    public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
    {
        var current = type;
        while (current is not null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
