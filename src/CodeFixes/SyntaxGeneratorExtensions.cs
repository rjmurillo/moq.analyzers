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
            ThrowIfIndexOutOfRangeForInsert(index, arguments.Count);
            arguments = arguments.InsertRange(index, items.OfType<ArgumentSyntax>());
            return invocation.WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }

        if (syntax is BaseObjectCreationExpressionSyntax creation)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList?.Arguments ?? [];
            ThrowIfIndexOutOfRangeForInsert(index, arguments.Count);
            arguments = arguments.InsertRange(index, items.OfType<ArgumentSyntax>());
            return creation.WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }

        throw new ArgumentException($"Must be of type {nameof(InvocationExpressionSyntax)} or {nameof(BaseObjectCreationExpressionSyntax)} but is of type {syntax.GetType().Name}", nameof(syntax));
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
            ThrowIfIndexOutOfRangeForReplace(index, arguments.Count);
            arguments = arguments.RemoveAt(index).Insert(index, argument);
            return invocation.WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }

        if (syntax is BaseObjectCreationExpressionSyntax creation)
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList?.Arguments ?? [];
            ThrowIfIndexOutOfRangeForReplace(index, arguments.Count);
            arguments = arguments.RemoveAt(index).Insert(index, argument);
            return creation.WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }

        throw new ArgumentException($"Must be of type {nameof(InvocationExpressionSyntax)} or {nameof(BaseObjectCreationExpressionSyntax)} but is of type {syntax.GetType().Name}", nameof(syntax));
    }

    private static void ThrowIfIndexOutOfRangeForInsert(int index, int count)
    {
        if (index < 0 || index > count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} must be between 0 and {count}.");
        }
    }

    private static void ThrowIfIndexOutOfRangeForReplace(int index, int count)
    {
        if (count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Cannot replace argument at index {index} because there are no arguments.");
        }

        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} must be between 0 and {count - 1}.");
        }
    }
}
