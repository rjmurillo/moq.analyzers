namespace Moq.Analyzers;

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
}
