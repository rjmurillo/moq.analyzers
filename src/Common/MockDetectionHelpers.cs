using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers.Common;

/// <summary>
/// Shared helper methods for detecting Moq mock creation patterns across analyzers.
/// </summary>
internal static class MockDetectionHelpers
{
    /// <summary>
    /// Determines if the operation is a valid <c>Mock{T}</c> object creation and extracts the mocked type.
    /// </summary>
    /// <param name="creation">The object creation operation to check.</param>
    /// <param name="knownSymbols">The Moq known symbols.</param>
    /// <param name="mockedType">When successful, contains the mocked type.</param>
    /// <returns><see langword="true"/> if this is a valid <c>Mock{T}</c> creation; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidMockCreation(
        IObjectCreationOperation creation,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (creation.Type is null || creation.Constructor is null || !creation.Type.IsInstanceOf(knownSymbols.Mock1))
        {
            return false;
        }

        return TryGetMockedTypeFromGeneric(creation.Type, out mockedType);
    }

    /// <summary>
    /// Attempts to extract the mocked type argument from a generic <c>Mock{T}</c> type.
    /// </summary>
    /// <param name="type">The type to extract from.</param>
    /// <param name="mockedType">When successful, contains the mocked type.</param>
    /// <returns><see langword="true"/> if the mocked type was extracted; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetMockedTypeFromGeneric(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1)
        {
            return false;
        }

        mockedType = namedType.TypeArguments[0];
        return true;
    }

    /// <summary>
    /// Checks if the method symbol represents a static <c>Mock.Of{T}()</c> method.
    /// </summary>
    /// <param name="targetMethod">The method symbol to check.</param>
    /// <param name="knownSymbols">The Moq known symbols.</param>
    /// <returns><see langword="true"/> if this is the <c>Mock.Of</c> method; otherwise, <see langword="false"/>.</returns>
    public static bool IsMockOfMethod(IMethodSymbol? targetMethod, MoqKnownSymbols knownSymbols)
    {
        if (targetMethod is null || !targetMethod.IsStatic)
        {
            return false;
        }

        if (!string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal))
        {
            return false;
        }

        return targetMethod.ContainingType is not null &&
               targetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// Attempts to locate the type argument in the syntax tree for precise diagnostic reporting.
    /// </summary>
    /// <param name="operation">The operation being analyzed.</param>
    /// <param name="fallbackSyntax">The fallback syntax node if the type argument cannot be found.</param>
    /// <returns>The location of the type argument, or the fallback syntax location.</returns>
    public static Location GetDiagnosticLocation(IOperation operation, SyntaxNode fallbackSyntax)
    {
        TypeSyntax? typeArgument = operation.Syntax
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .FirstOrDefault()?
            .TypeArgumentList?
            .Arguments
            .FirstOrDefault();

        return typeArgument?.GetLocation() ?? fallbackSyntax.GetLocation();
    }
}
