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
    /// Determines if the operation is a valid <c>Mock{T}</c> object creation and extracts the mocked type.
    /// </summary>
    /// <param name="creation">The object creation operation.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="mockedType">When successful, the mocked type; otherwise, null.</param>
    /// <returns>True if this is a valid <c>Mock{T}</c> creation; otherwise, false.</returns>
    internal static bool IsValidMockCreation(IObjectCreationOperation creation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (creation.Type is null || creation.Constructor is null || !creation.Type.IsInstanceOf(knownSymbols.Mock1))
        {
            return false;
        }

        return TryGetMockedTypeFromGeneric(creation.Type, out mockedType);
    }

    /// <summary>
    /// Determines if the operation is a valid <c>Mock.Of{T}()</c> invocation and extracts the mocked type.
    /// </summary>
    /// <param name="invocation">The invocation operation.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="mockedType">When successful, the mocked type; otherwise, null.</param>
    /// <returns>True if this is a valid <c>Mock.Of{T}()</c> invocation; otherwise, false.</returns>
    internal static bool IsValidMockOfInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
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
    /// Determines if the operation is a valid <c>Mock.Of{T}()</c> or <c>MockRepository.Create{T}()</c> invocation
    /// and extracts the mocked type.
    /// </summary>
    /// <param name="invocation">The invocation operation.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="mockedType">When successful, the mocked type; otherwise, null.</param>
    /// <returns>True if this is a valid mock invocation; otherwise, false.</returns>
    internal static bool IsValidMockInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        IMethodSymbol targetMethod = invocation.TargetMethod;

        bool isMockOf = IsValidMockOfMethod(targetMethod, knownSymbols);
        bool isMockRepositoryCreate = !isMockOf && targetMethod.IsInstanceOf(knownSymbols.MockRepositoryCreate);

        if (!isMockOf && !isMockRepositoryCreate)
        {
            return false;
        }

        if (targetMethod.TypeArguments.Length == 1)
        {
            mockedType = targetMethod.TypeArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the method symbol represents a <c>Mock.Of{T}()</c> method.
    /// </summary>
    /// <param name="targetMethod">The method symbol to check.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <returns>True if the method is <c>Mock.Of{T}()</c>; otherwise, false.</returns>
    internal static bool IsValidMockOfMethod(IMethodSymbol? targetMethod, MoqKnownSymbols knownSymbols)
        => targetMethod is not null && targetMethod.IsInstanceOf(knownSymbols.MockOf);

    /// <summary>
    /// Attempts to extract the mocked type argument from a generic <c>Mock{T}</c> type.
    /// </summary>
    /// <param name="type">The type symbol to extract from.</param>
    /// <param name="mockedType">When successful, the mocked type; otherwise, null.</param>
    /// <returns>True if the mocked type was extracted; otherwise, false.</returns>
    internal static bool TryGetMockedTypeFromGeneric(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? mockedType)
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
    internal static Location GetDiagnosticLocation(IOperation operation, SyntaxNode fallbackSyntax)
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
