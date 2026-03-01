using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers.Common;

/// <summary>
/// Provides helper methods for detecting mock creation patterns in Moq.
/// </summary>
internal static class MockDetectionHelpers
{
    /// <summary>
    /// Determines if the operation is a valid Mock{T} object creation and extracts the mocked type.
    /// </summary>
    /// <param name="creation">The object creation operation.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="mockedType">When successful, the mocked type; otherwise, null.</param>
    /// <returns>True if this is a valid Mock{T} creation; otherwise, false.</returns>
    public static bool IsValidMockCreation(IObjectCreationOperation creation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (creation.Type is null || creation.Constructor is null || !creation.Type.IsInstanceOf(knownSymbols.Mock1))
        {
            return false;
        }

        return TryGetMockedTypeFromGeneric(creation.Type, out mockedType);
    }

    /// <summary>
    /// Determines if the operation is a valid Mock.Of{T}() invocation and extracts the mocked type.
    /// </summary>
    /// <param name="invocation">The invocation operation.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="mockedType">When successful, the mocked type; otherwise, null.</param>
    /// <returns>True if this is a valid Mock.Of{T}() invocation; otherwise, false.</returns>
    public static bool IsValidMockOfInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (!IsValidMockOfMethod(invocation.TargetMethod, knownSymbols))
        {
            return false;
        }

        if (invocation.TargetMethod.TypeArguments.Length == 1)
        {
            mockedType = invocation.TargetMethod.TypeArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the method symbol represents a static Mock.Of{T}() method.
    /// </summary>
    /// <param name="targetMethod">The method symbol to check.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <returns>True if the method is Mock.Of{T}(); otherwise, false.</returns>
    public static bool IsValidMockOfMethod(IMethodSymbol? targetMethod, MoqKnownSymbols knownSymbols)
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
    /// Attempts to extract the mocked type argument from a generic Mock{T} type.
    /// </summary>
    /// <param name="type">The type symbol to extract from.</param>
    /// <param name="mockedType">When successful, the mocked type; otherwise, null.</param>
    /// <returns>True if the mocked type was extracted; otherwise, false.</returns>
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
    /// Attempts to locate the type argument in the syntax tree for precise diagnostic reporting.
    /// </summary>
    /// <param name="operation">The operation being analyzed.</param>
    /// <param name="fallbackSyntax">The fallback syntax node if type argument cannot be found.</param>
    /// <returns>The location for reporting the diagnostic.</returns>
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
