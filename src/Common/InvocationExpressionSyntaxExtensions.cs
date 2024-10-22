using System.Diagnostics.CodeAnalysis;

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

    [SuppressMessage("Performance", "ECS0900:Minimize boxing and unboxing", Justification = "SyntaxFactory API requires an array.")]
    internal static InvocationExpressionSyntax PrependArgumentListArguments(this InvocationExpressionSyntax syntax, params ArgumentSyntax[] items)
    {
        return syntax.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([..items, ..syntax.ArgumentList.Arguments.ToArray()])));
    }
}
