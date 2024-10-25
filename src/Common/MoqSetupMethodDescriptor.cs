namespace Moq.Analyzers.Common;

/// <summary>
/// A class that, given a <see cref="SemanticModel"/> and a <see cref="MemberAccessExpressionSyntax"/>, determines if
/// it is a call to the Moq `Mock.Setup()` method.
/// </summary>
internal class MoqSetupMethodDescriptor : MoqMethodDescriptorBase
{
    private static readonly string MethodName = "Setup";

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
}
