using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers;

internal static class SyntaxExtensions
{
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
