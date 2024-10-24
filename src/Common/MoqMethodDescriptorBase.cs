﻿namespace Moq.Analyzers.Common;

/// <summary>
/// A base that provides common functionality for identifying if a given <see cref="SyntaxNode"/>
/// is a specific Moq method.
/// </summary>
/// <remarks>
/// Currently the <see cref="IsMatch(SemanticModel, MemberAccessExpressionSyntax, CancellationToken)"/> abstract method
/// is specific to <see cref="MemberAccessExpressionSyntax"/> because that's the only type of syntax in use. I expect we'll need
/// to loosen this restriction if we start using other types of syntax.
/// </remarks>
internal abstract class MoqMethodDescriptorBase
{
    private static readonly string ContainingNamespace = WellKnownMoqNames.MoqNamespace;
    private static readonly string ContainingType = WellKnownMoqNames.MockTypeName;

    public abstract bool IsMatch(SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccessSyntax, CancellationToken cancellationToken);

    protected static bool IsFastMatch(MemberAccessExpressionSyntax memberAccessSyntax, ReadOnlySpan<char> methodName) => memberAccessSyntax.Name.Identifier.Text.AsSpan().SequenceEqual(methodName);

    protected static bool IsContainedInMockType(IMethodSymbol methodSymbol) => IsInMoqNamespace(methodSymbol) && IsInMockType(methodSymbol);

    private static bool IsInMoqNamespace(ISymbol symbol) => symbol.ContainingNamespace.Name.AsSpan().SequenceEqual(ContainingNamespace.AsSpan());

    private static bool IsInMockType(ISymbol symbol) => symbol.ContainingType.Name.AsSpan().SequenceEqual(ContainingType.AsSpan());
}
