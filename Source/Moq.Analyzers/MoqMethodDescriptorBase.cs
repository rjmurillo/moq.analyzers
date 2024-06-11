namespace Moq.Analyzers;

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
