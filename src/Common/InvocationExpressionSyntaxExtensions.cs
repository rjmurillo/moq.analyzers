namespace Moq.Analyzers.Common;

/// <summary>
/// Extension methods for <see cref="InvocationExpressionSyntax"/>s.
/// </summary>
internal static class InvocationExpressionSyntaxExtensions
{
    internal static InvocationExpressionSyntax? FindMockedMethodInvocationFromSetupMethod(this InvocationExpressionSyntax? setupInvocation)
    {
        return GetSetupLambdaArgument(setupInvocation)?.Body as InvocationExpressionSyntax;
    }

    internal static ExpressionSyntax? FindMockedMemberExpressionFromSetupMethod(this InvocationExpressionSyntax? setupInvocation)
    {
        return GetSetupLambdaArgument(setupInvocation)?.Body as ExpressionSyntax;
    }

    /// <summary>
    /// Walks up the Moq fluent chain to find the Setup invocation.
    /// Handles patterns like <c>mock.Setup(...).Returns(...)</c> and
    /// <c>mock.Setup(...).Callback(...).Returns(...)</c>.
    /// </summary>
    /// <param name="receiver">The receiver expression to start walking from (typically the expression before the Returns call).</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="knownSymbols">The known Moq symbols for type checking.</param>
    /// <returns>The Setup invocation if found; otherwise, <see langword="null"/>.</returns>
    internal static InvocationExpressionSyntax? FindSetupInvocation(this ExpressionSyntax receiver, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
    {
        ExpressionSyntax current = receiver;

        // Moq fluent chains are short (Setup.Callback.Returns at most 3-4 deep).
        // Guard against pathological syntax trees.
        for (int depth = 0; depth < 10; depth++)
        {
            ExpressionSyntax unwrapped = current.WalkDownParentheses();

            if (unwrapped is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax candidateMemberAccess } candidateInvocation)
            {
                return null;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(candidateInvocation);
            if (symbolInfo.Symbol != null && symbolInfo.Symbol.IsMoqSetupMethod(knownSymbols))
            {
                return candidateInvocation;
            }

            // Continue walking up the chain (past Callback, etc.)
            current = candidateMemberAccess.Expression;
        }

        return null;
    }

    private static LambdaExpressionSyntax? GetSetupLambdaArgument(InvocationExpressionSyntax? setupInvocation)
    {
        if (setupInvocation is null || setupInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        return setupInvocation.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
    }
}
