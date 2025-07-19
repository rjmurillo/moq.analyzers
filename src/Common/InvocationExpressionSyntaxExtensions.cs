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
    /// Determines if an invocation is a Raises method call in a Moq fluent API chain.
    /// This method uses symbol-based detection to verify the method belongs to IRaiseable or IRaiseableAsync.
    /// </summary>
    /// <param name="invocation">The invocation expression to check.</param>
    /// <param name="semanticModel">The semantic model for symbol resolution.</param>
    /// <param name="knownSymbols">The known Moq symbols for type checking.</param>
    /// <returns><see langword="true"/> if the invocation is a Raises method call; otherwise, <see langword="false"/>.</returns>
    internal static bool IsRaisesMethodCall(this InvocationExpressionSyntax invocation, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
    {
        // Check if the method being called is named "Raises" or "RaisesAsync"
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        string methodName = memberAccess.Name.Identifier.ValueText;
        if (!methodName.Equals("Raises", StringComparison.Ordinal) && !methodName.Equals("RaisesAsync", StringComparison.Ordinal))
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

        // Fallback: Additional validation to ensure it's part of a Moq fluent API chain
        // by checking if it follows a Setup() call (original logic as fallback)
        ExpressionSyntax? expression = memberAccess.Expression;
        bool isPartOfMoqChain = false;

        while (expression is InvocationExpressionSyntax parentInvocation)
        {
            if (parentInvocation.Expression is MemberAccessExpressionSyntax parentMemberAccess &&
                string.Equals(parentMemberAccess.Name.Identifier.ValueText, "Setup", StringComparison.Ordinal))
            {
                isPartOfMoqChain = true;
                break;
            }

            expression = (parentInvocation.Expression as MemberAccessExpressionSyntax)?.Expression;
            if (expression == null)
            {
                break;
            }
        }

        return isPartOfMoqChain;
    }
}
