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
    /// This method uses string-based detection with validation that the call is part of a Setup() chain.
    /// </summary>
    /// <param name="invocation">The invocation expression to check.</param>
    /// <returns><see langword="true"/> if the invocation is a Raises method call; otherwise, <see langword="false"/>.</returns>
    internal static bool IsRaisesMethodCall(this InvocationExpressionSyntax invocation)
    {
        // Check if the method being called is named "Raises" or "RaisesAsync"
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // TODO: The symbol-based detection is not working correctly because the containing type
        // for the Raises method might be different than expected (e.g., due to Moq's internal
        // implementation details or version differences). Need to investigate the actual type
        // hierarchy. For now, fallback to string-based detection to ensure functionality.
        string methodName = memberAccess.Name.Identifier.ValueText;
        if (!methodName.Equals("Raises", StringComparison.Ordinal) && !methodName.Equals("RaisesAsync", StringComparison.Ordinal))
        {
            return false;
        }

        // Additional validation: ensure it's part of a Moq fluent API chain
        // by checking if it follows a Setup() call
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
