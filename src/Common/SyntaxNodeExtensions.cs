namespace Moq.Analyzers.Common;

internal static class SyntaxNodeExtensions
{
    /// <summary>
    /// Finds the location of the first descendant node of type <typeparamref name="TSyntax"/> whose symbol matches <paramref name="memberSymbol"/>.
    /// </summary>
    /// <typeparam name="TSyntax">The <see cref="SyntaxNode"/> type to search for.</typeparam>
    /// <param name="syntax">The root <see cref="SyntaxNode"/> to search from.</param>
    /// <param name="memberSymbol">The <see cref="ISymbol"/> to match.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for symbol resolution.</param>
    /// <returns>The location of the matching node, or <see langword="null"/> if not found.</returns>
    internal static Location? FindLocation<TSyntax>(this SyntaxNode syntax, ISymbol memberSymbol, SemanticModel? semanticModel)
        where TSyntax : SyntaxNode
    {
        TSyntax? node = syntax.DescendantNodes()
            .OfType<TSyntax>()
            .FirstOrDefault(n =>
            {
                ISymbol? symbol = semanticModel?.GetSymbolInfo(n).Symbol;
                return SymbolEqualityComparer.Default.Equals(symbol, memberSymbol);
            });

        return node != null
            ? Location.Create(syntax.SyntaxTree, node.Span)
            : null;
    }

    /// <summary>
    /// Returns the parent node, skipping any intermediate <see cref="ParenthesizedExpressionSyntax"/> wrappers.
    /// This handles cases like <c>(mock.Setup(...)).Returns()</c> where parentheses wrap an invocation.
    /// </summary>
    /// <param name="node">The node whose logical parent to find.</param>
    /// <returns>The first non-parenthesized ancestor, or <see langword="null"/> if none exists.</returns>
    internal static SyntaxNode? GetParentSkippingParentheses(this SyntaxNode node)
    {
        SyntaxNode? parent = node.Parent;
        while (parent is ParenthesizedExpressionSyntax)
        {
            parent = parent.Parent;
        }

        return parent;
    }

    /// <summary>
    /// Unwraps any <see cref="ParenthesizedExpressionSyntax"/> wrappers to get the inner expression.
    /// This handles cases like <c>(mock.Setup(...)).Returns()</c> when walking DOWN the syntax tree.
    /// </summary>
    /// <param name="expression">The expression to unwrap.</param>
    /// <returns>The innermost non-parenthesized expression.</returns>
    /// <remarks>
    /// Forked from Roslyn ExpressionSyntaxExtensions.WalkDownParentheses.
    /// See https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Extensions/ExpressionSyntaxExtensions.cs.
    /// </remarks>
    internal static ExpressionSyntax WalkDownParentheses(this ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenExpression)
        {
            expression = parenExpression.Expression;
        }

        return expression;
    }
}
