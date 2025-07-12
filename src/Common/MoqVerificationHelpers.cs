using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Shared helper methods for Moq verification analysis and code fixes.
/// </summary>
internal static class MoqVerificationHelpers
{
    /// <summary>
    /// Extracts a lambda function operation from a Moq verification argument.
    /// </summary>
    /// <param name="argumentOperation">The argument operation to extract from.</param>
    /// <returns>The lambda operation if found, otherwise null.</returns>
    public static IAnonymousFunctionOperation? ExtractLambdaFromArgument(IOperation argumentOperation)
    {
        argumentOperation = argumentOperation.WalkDownImplicitConversion();

        // Handle delegate conversions (e.g., VerifySet(x => { ... }))
        if (argumentOperation is IDelegateCreationOperation delegateCreation &&
            delegateCreation.Target is IAnonymousFunctionOperation lambdaOp)
        {
            return lambdaOp;
        }

        return argumentOperation as IAnonymousFunctionOperation;
    }

    /// <summary>
    /// Extracts the property symbol from a VerifySet lambda expression.
    /// </summary>
    /// <param name="lambdaOperation">The lambda operation containing the property assignment.</param>
    /// <returns>The property symbol if found, otherwise null.</returns>
    public static ISymbol? ExtractPropertyFromVerifySetLambda(IAnonymousFunctionOperation lambdaOperation)
    {
        foreach (IOperation op in lambdaOperation.Body.Operations)
        {
            if (op is IExpressionStatementOperation exprStmt)
            {
                IAssignmentOperation? assignOp = exprStmt.Operation as IAssignmentOperation
                    ?? exprStmt.Operation as ISimpleAssignmentOperation;

                if (assignOp?.Target is IPropertyReferenceOperation propRef)
                {
                    return propRef.Property;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the mocked member symbol from a Moq Verify/Setup invocation.
    /// </summary>
    /// <param name="moqInvocation">The invocation operation.</param>
    /// <returns>The mocked member symbol if found, otherwise null.</returns>
    public static ISymbol? TryGetMockedMemberSymbol(IInvocationOperation moqInvocation)
    {
        if (moqInvocation.Arguments.Length == 0)
        {
            return null;
        }

        // For code fix, we only need to handle the simple case (first argument)
        // since the analyzer already validates the more complex scenarios
        IAnonymousFunctionOperation? lambdaOperation = ExtractLambdaFromArgument(moqInvocation.Arguments[0].Value);

        return lambdaOperation?.Body.GetReferencedMemberSymbolFromLambda();
    }
}
