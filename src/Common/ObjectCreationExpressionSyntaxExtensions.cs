using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers.Common;

internal static class ObjectCreationExpressionSyntaxExtensions
{
    // TODO: Can this be removed?
    [SuppressMessage("Performance", "ECS0900:Minimize boxing and unboxing", Justification = "SyntaxFactory API requires an array.")]
    internal static ObjectCreationExpressionSyntax PrependArgumentListArguments(this ObjectCreationExpressionSyntax syntax, params ArgumentSyntax[] items)
    {
        ArgumentSyntax[] arguments = syntax.ArgumentList?.Arguments.ToArray() ?? [];

        return syntax.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([..items, ..arguments])));
    }
}
