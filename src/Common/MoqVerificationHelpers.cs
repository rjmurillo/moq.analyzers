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
}
