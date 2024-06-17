namespace Moq.Analyzers;

/// <summary>
/// A class that, given a <see cref="SemanticModel"/> and a <see cref="MemberAccessExpressionSyntax"/>, determines if
/// it is a call to the Moq `Mock.Setup()` method.
/// </summary>
internal class MoqSetupMethodDescriptor : MoqMethodDescriptorBase
{
    private const string MethodName = "Setup";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
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
