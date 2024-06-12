namespace Moq.Analyzers;

/// <summary>
/// A base that that provides common functionality for identifying if a given <see cref="SyntaxNode"/>
/// is a specific Moq method.
/// </summary>
/// <remarks>
/// Currently the <see cref="IsMatch(SemanticModel, MemberAccessExpressionSyntax, CancellationToken)"/> abstract method
/// is specific to <see cref="MemberAccessExpressionSyntax"/> because that's the only type of syntax in use. I expect we'll need
/// to loosen this restriction if we start using other types of syntax.
/// </remarks>
internal abstract class MoqMethodDescriptorBase
{
    private const string ContainingNamespace = "Moq";
    private const string ContainingType = "Mock";

    public abstract bool IsMatch(SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccessSyntax, CancellationToken cancellationToken);

    protected static bool IsFastMatch(MemberAccessExpressionSyntax memberAccessSyntax, ReadOnlySpan<char> methodName)
    {
        return memberAccessSyntax.Name.Identifier.Text.AsSpan().SequenceEqual(methodName);
    }

    protected static bool IsContainedInMockType(IMethodSymbol methodSymbol)
    {
        return IsInMoqNamespace(methodSymbol) && IsInMockType(methodSymbol);
    }

    private static bool IsInMoqNamespace(ISymbol symbol)
    {
        return symbol.ContainingNamespace.Name.AsSpan().SequenceEqual(ContainingNamespace.AsSpan());
    }

    private static bool IsInMockType(ISymbol symbol)
    {
        return symbol.ContainingType.Name.AsSpan().SequenceEqual(ContainingType.AsSpan());
    }
}
