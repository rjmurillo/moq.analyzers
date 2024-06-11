namespace Moq.Analyzers;

internal class MoqAsMethodDescriptor : MoqMethodDescriptorBase
{
    private const string ContainingNamespace = "Moq";
    private const string ContainingType = "Mock";
    private const string MethodName = "As";

    public override bool IsMatch(SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccessSyntax, CancellationToken cancellationToken)
    {
        if (!IsFastMatch(memberAccessSyntax))
        {
            return false;
        }

        ISymbol? symbol = semanticModel.GetSymbolInfo(memberAccessSyntax, cancellationToken).Symbol;

        if (symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        if (!string.Equals(methodSymbol.ContainingNamespace?.ToString(), ContainingNamespace, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(methodSymbol.ContainingType?.Name, ContainingType, StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(methodSymbol.Name, MethodName, StringComparison.Ordinal) && methodSymbol.IsGenericMethod;
    }

    private static bool IsFastMatch(MemberAccessExpressionSyntax memberAccessSyntax)
    {
        return string.Equals(memberAccessSyntax.Name.Identifier.Text, MethodName, StringComparison.Ordinal);
    }
}
