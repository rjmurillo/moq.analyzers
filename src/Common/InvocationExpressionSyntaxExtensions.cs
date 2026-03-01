namespace Moq.Analyzers.Common;

/// <summary>
/// Extension methods for <see cref="InvocationExpressionSyntax"/>s.
/// </summary>
internal static class InvocationExpressionSyntaxExtensions
{
    internal static InvocationExpressionSyntax? FindMockedMethodInvocationFromSetupMethod(this InvocationExpressionSyntax? setupInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as InvocationExpressionSyntax;
    }

    internal static ExpressionSyntax? FindMockedMemberExpressionFromSetupMethod(this InvocationExpressionSyntax? setupInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as ExpressionSyntax;
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

    /// <summary>
    /// Determines if an invocation is a Raises method call using symbol-based detection.
    /// This method verifies the method belongs to IRaiseable or IRaiseableAsync.
    /// </summary>
    /// <param name="invocation">The invocation expression to check.</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="knownSymbols">The known Moq symbols for type checking.</param>
    /// <returns><see langword="true"/> if the invocation is a Raises method call; otherwise, <see langword="false"/>.</returns>
    internal static bool IsRaisesMethodCall(this InvocationExpressionSyntax invocation, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Use symbol-based detection to verify this is a proper Moq Raises method
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol?.IsMoqRaisesMethod(knownSymbols) == true)
        {
            return true;
        }

        // Check candidate symbols in case of overload resolution failure
        if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure &&
            symbolInfo.CandidateSymbols.Any(symbol => symbol.IsMoqRaisesMethod(knownSymbols)))
        {
            return true;
        }

        return false;
    }
}
