using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers.Common;

internal static class IMethodSymbolExtensions
{
    /// <summary>
    /// Get all overloads of a given <see cref="IMethodSymbol"/>.
    /// </summary>
    /// <param name="method">The method to inspect for overloads.</param>
    /// <param name="comparer">
    /// The <see cref="SymbolEqualityComparer"/> to use for the comparison. Defaults to <see cref="SymbolEqualityComparer.Default"/>.
    /// </param>
    /// <returns>
    /// A collection of <see cref="IMethodSymbol"/> representing the overloads of the given method.
    /// </returns>
    public static IEnumerable<IMethodSymbol> Overloads(this IMethodSymbol? method, SymbolEqualityComparer? comparer = null)
    {
        comparer ??= SymbolEqualityComparer.Default;

        IEnumerable<IMethodSymbol>? methods = method?.ContainingType?.GetMembers(method.Name).OfType<IMethodSymbol>();

        if (methods is not null)
        {
            foreach (IMethodSymbol member in methods)
            {
                if (!comparer.Equals(member, method))
                {
                    yield return member;
                }
            }
        }
    }

    // TODO: Maybe revert the double out params?

    /// <summary>
    /// Check if, given a set of overloads, any overload has a parameter of the given type.
    /// </summary>
    /// <param name="method">The method to inspect for overloads.</param>
    /// <param name="overloads">The set of candidate methods to check.</param>
    /// <param name="type">The type to check for in the parameters.</param>
    /// <param name="methodMatch">The matching method overload. <see langword="null"/> if no matches.</param>
    /// <param name="parameterMatch">The matching parameter. <see langword="null"/> if no matches.</param>
    /// <param name="comparer">The <see cref="SymbolEqualityComparer"/> to use for equality.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to use to cancel long running operations.</param>
    /// <returns><see langword="true"/> if a method in <paramref name="overloads"/> has a parameter of type <paramref name="type"/>. Otherwise <see langword="false"/>.</returns>
    public static bool TryGetOverloadWithParameterOfType(this IMethodSymbol method, IEnumerable<IMethodSymbol> overloads, INamedTypeSymbol type, [NotNullWhen(true)] out IMethodSymbol? methodMatch, [NotNullWhen(true)] out IParameterSymbol? parameterMatch, SymbolEqualityComparer? comparer = null, CancellationToken cancellationToken = default)
    {
        comparer ??= SymbolEqualityComparer.Default;

        foreach (IMethodSymbol overload in overloads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (comparer.Equals(method, overload))
            {
                continue;
            }

            foreach (IParameterSymbol parameter in overload.Parameters)
            {
                if (comparer.Equals(parameter.Type, type))
                {
                    methodMatch = overload;
                    parameterMatch = parameter;
                    return true;
                }
            }
        }

        methodMatch = null;
        parameterMatch = null;
        return false;
    }

    /// <inheritdoc cref="TryGetOverloadWithParameterOfType(IMethodSymbol, IEnumerable{IMethodSymbol}, INamedTypeSymbol, out IMethodSymbol, out IParameterSymbol, SymbolEqualityComparer?, CancellationToken)"/>
    public static bool TryGetOverloadWithParameterOfType(this IMethodSymbol method, INamedTypeSymbol type, [NotNullWhen(true)] out IMethodSymbol? methodMatch, [NotNullWhen(true)] out IParameterSymbol? parameterMatch, SymbolEqualityComparer? comparer = null, CancellationToken cancellationToken = default)
    {
        return method.TryGetOverloadWithParameterOfType(method.Overloads(), type, out methodMatch, out parameterMatch, comparer, cancellationToken);
    }

    public static bool TryGetParameterOfType(this IMethodSymbol method, INamedTypeSymbol type, [NotNullWhen(true)] out IParameterSymbol? match, SymbolEqualityComparer? comparer = null, CancellationToken cancellationToken = default)
    {
        comparer ??= SymbolEqualityComparer.Default;

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (comparer.Equals(parameter.Type, type))
            {
                match = parameter;
                return true;
            }
        }

        match = null;
        return false;
    }
}
