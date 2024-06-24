using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers;

/// <summary>
/// Extensions methods for <see cref="NameSyntax"/>s.
/// </summary>
internal static class NameSyntaxExtensions
{
    /// <summary>
    /// Tries to get the generic arguments of a given <see cref="NameSyntax"/>.
    /// </summary>
    /// <param name="syntax">The syntax to inspect.</param>
    /// <param name="typeArguments">The collection of <see cref="TypeSyntax"/> elements on the <paramref name="syntax"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="syntax"/> has generic / type parameters; <see langword="false"/> otherwise.</returns>
    /// <example>
    /// x.As&lt;ISampleInterface&gt;() returns <see langword="true"/> and <paramref name="typeArguments"/> will contain <c>ISampleInterface</c>.
    /// </example>
    /// <example>
    /// x.As() returns <see langword="false"/> and <paramref name="typeArguments"/> will be empty.
    /// </example>
    public static bool TryGetGenericArguments(this NameSyntax syntax, [NotNullWhen(true)] out SeparatedSyntaxList<TypeSyntax> typeArguments)
    {
        if (syntax is GenericNameSyntax genericName)
        {
            typeArguments = genericName.TypeArgumentList.Arguments;
            return true;
        }

        typeArguments = default;
        return false;
    }
}
