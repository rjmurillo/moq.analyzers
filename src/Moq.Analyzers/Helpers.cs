namespace Moq.Analyzers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "AV1708:Type name contains term that should be avoided", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
internal static class Helpers
{
    internal static InvocationExpressionSyntax? FindMockedMethodInvocationFromSetupMethod(InvocationExpressionSyntax? setupInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as InvocationExpressionSyntax;
    }

    internal static ExpressionSyntax? FindMockedMemberExpressionFromSetupMethod(InvocationExpressionSyntax? setupInvocation)
    {
        LambdaExpressionSyntax? setupLambdaArgument = setupInvocation?.ArgumentList.Arguments[0].Expression as LambdaExpressionSyntax;
        return setupLambdaArgument?.Body as ExpressionSyntax;
    }
}
