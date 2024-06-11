namespace Moq.Analyzers;

internal class MoqSetupMethodDescriptor : MoqMethodDescriptorBase
{
    private const string MethodName = "Setup";

    public override bool IsMatch(SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccessSyntax, CancellationToken cancellationToken)
    {
        if (!IsFastMatch(memberAccessSyntax, MethodName.AsSpan()))
        {
            return false;
        }

        ISymbol? symbol = semanticModel.GetSymbolInfo(memberAccessSyntax, cancellationToken).Symbol;

        if (symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        if (!IsContainedInMockType(methodSymbol))
        {
            return false;
        }

        return methodSymbol.Name.AsSpan().SequenceEqual(MethodName.AsSpan()) && methodSymbol.IsGenericMethod;
    }

    private static bool IsFastMatch(MemberAccessExpressionSyntax memberAccessSyntax)
    {
        return string.Equals(memberAccessSyntax.Name.Identifier.Text, MethodName, StringComparison.Ordinal);
    }
}
