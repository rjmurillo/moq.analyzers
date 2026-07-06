using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers.Common;

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
        IArgumentOperation? expressionArgument = GetArgumentForParameterOrdinal(moqInvocation, 0);

        IAnonymousFunctionOperation? lambdaOperation = expressionArgument is not null
            ? ExtractLambdaFromArgument(expressionArgument.Value)
            : null;

        return lambdaOperation?.Body.GetReferencedMemberSymbolFromLambda();
    }

    /// <summary>
    /// Extracts the mocked member from a Moq Setup/SetupSequence/Verify invocation and
    /// determines whether it is a member that Moq cannot mock.
    /// </summary>
    /// <param name="moqInvocation">The Setup/SetupSequence/Verify invocation operation.</param>
    /// <param name="knownSymbols">The known Moq symbols for this compilation.</param>
    /// <param name="mockedMemberSymbol">
    /// When this method returns <see langword="true"/>, the non-overridable mocked member.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the invocation lambda references a member that is not
    /// overridable/allowed for mocking; otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryGetNonOverridableMockedMember(
        IInvocationOperation moqInvocation,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out ISymbol? mockedMemberSymbol)
    {
        mockedMemberSymbol = null;

        ISymbol? candidate = TryGetMockedMemberSymbol(moqInvocation);
        if (candidate is null || candidate.IsOverridableOrAllowedMockMember(knownSymbols))
        {
            return false;
        }

        mockedMemberSymbol = candidate;
        return true;
    }

    /// <summary>
    /// Extracts the mocked member syntax node from a Moq Verify/Setup invocation.
    /// </summary>
    /// <param name="moqInvocation">The invocation operation.</param>
    /// <returns>The mocked member syntax node if found, otherwise null.</returns>
    public static SyntaxNode? TryGetMockedMemberSyntax(IInvocationOperation moqInvocation)
    {
        IArgumentOperation? expressionArgument = GetArgumentForParameterOrdinal(moqInvocation, 0);

        IAnonymousFunctionOperation? lambdaOperation = expressionArgument is not null
            ? ExtractLambdaFromArgument(expressionArgument.Value)
            : null;

        return lambdaOperation?.Body.GetReferencedMemberSyntaxFromLambda();
    }

    /// <summary>
    /// Gets the argument bound to the parameter at the given ordinal, regardless of source order.
    /// </summary>
    /// <param name="moqInvocation">The invocation operation.</param>
    /// <param name="ordinal">The zero-based parameter ordinal.</param>
    /// <returns>The matching argument, or null if none is bound to that ordinal.</returns>
    public static IArgumentOperation? GetArgumentForParameterOrdinal(IInvocationOperation moqInvocation, int ordinal)
    {
        System.Diagnostics.Debug.Assert(ordinal >= 0, "Parameter ordinal must be non-negative.");

        IArgumentOperation? match = null;
        foreach (IArgumentOperation argument in moqInvocation.Arguments)
        {
            if (argument.Parameter?.Ordinal == ordinal)
            {
                match = argument;
                break;
            }
        }

        return match;
    }
}
