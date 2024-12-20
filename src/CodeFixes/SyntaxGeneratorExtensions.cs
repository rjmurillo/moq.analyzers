using Microsoft.CodeAnalysis.Editing;

namespace Moq.CodeFixes;

internal static class SyntaxGeneratorExtensions
{
    public static SyntaxNode MemberAccessExpression(this SyntaxGenerator generator, IFieldSymbol fieldSymbol)
    {
        return generator.MemberAccessExpression(generator.TypeExpression(fieldSymbol.Type), generator.IdentifierName(fieldSymbol.Name));
    }

    public static SyntaxNode InsertArguments(this SyntaxGenerator generator, SyntaxNode syntax, int index, params SyntaxNode[] items)
    {
        if (Array.Exists(items, item => item is not ArgumentSyntax))
        {
            throw new ArgumentException("Must all be of type ArgumentSyntax", nameof(items));
        }

        if (syntax is InvocationExpressionSyntax invocation)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            arguments = arguments.InsertRange(index, items.OfType<ArgumentSyntax>());

            syntax = syntax.ReplaceNode(invocation.ArgumentList, invocation.ArgumentList.WithArguments(arguments));

            return syntax;
        }

        if (syntax is ObjectCreationExpressionSyntax creation)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList?.Arguments ?? [];

            arguments = arguments.InsertRange(index, items.OfType<ArgumentSyntax>());
            ArgumentListSyntax argumentList = SyntaxFactory.ArgumentList(arguments);

            syntax = syntax.ReplaceNode(creation, creation.WithArgumentList(argumentList));

            return syntax;
        }

        throw new ArgumentException($"Must be of type {nameof(InvocationExpressionSyntax)} or {nameof(ObjectCreationExpressionSyntax)} but is of type {syntax.GetType().Name}", nameof(syntax));
    }

    public static SyntaxNode ReplaceArgument(this SyntaxGenerator generator, SyntaxNode syntax, int index, SyntaxNode item)
    {
        if (item is not ArgumentSyntax argument)
        {
            throw new ArgumentException("Must be of type ArgumentSyntax", nameof(item));
        }

        if (syntax is InvocationExpressionSyntax invocation)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            arguments = arguments.RemoveAt(index).Insert(index, argument);

            syntax = syntax.ReplaceNode(invocation.ArgumentList, invocation.ArgumentList.WithArguments(arguments));

            return syntax;
        }

        if (syntax is ObjectCreationExpressionSyntax creation)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList?.Arguments ?? [];

            arguments = arguments.RemoveAt(index).Insert(index, argument);
            ArgumentListSyntax argumentList = SyntaxFactory.ArgumentList(arguments);

            syntax = syntax.ReplaceNode(creation, creation.WithArgumentList(argumentList));

            return syntax;
        }

        throw new ArgumentException($"Must be of type {nameof(InvocationExpressionSyntax)} or {nameof(ObjectCreationExpressionSyntax)} but is of type {syntax.GetType().Name}", nameof(syntax));
    }
}
