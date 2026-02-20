using System.Diagnostics.CodeAnalysis;

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
    /// Walks an expression up through any <see cref="ParenthesizedExpressionSyntax"/> wrappers,
    /// returning the outermost parenthesized wrapper (or the expression itself if not wrapped).
    /// This handles cases like <c>(mock.Setup(x => x.M())).Returns(42)</c> when walking UP
    /// from the Setup invocation through enclosing parentheses.
    /// </summary>
    /// <param name="expression">The expression to walk up from.</param>
    /// <returns>The outermost parenthesized wrapper, or <paramref name="expression"/> if not wrapped.</returns>
    /// <remarks>
    /// Forked from Roslyn ExpressionSyntaxExtensions.WalkUpParentheses.
    /// See https://github.com/dotnet/roslyn/blob/1a693dfd634d96d8226c14ead7992c4e24a2880f/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Extensions/ExpressionSyntaxExtensions.cs.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(expression))]
    internal static ExpressionSyntax? WalkUpParentheses(this ExpressionSyntax? expression)
    {
        while (expression?.Parent is ParenthesizedExpressionSyntax parentExpr)
        {
            expression = parentExpr;
        }

        return expression;
    }

    /// <summary>
    /// Unwraps any <see cref="ParenthesizedExpressionSyntax"/> wrappers to get the inner expression.
    /// This handles cases like <c>((mock.Setup(x => x.M()))).Callback(() => { })</c> when walking
    /// DOWN from the parenthesized wrapper to find the inner Setup invocation.
    /// </summary>
    /// <param name="expression">The expression to unwrap.</param>
    /// <returns>The innermost non-parenthesized expression.</returns>
    /// <remarks>
    /// Forked from Roslyn ExpressionSyntaxExtensions.WalkDownParentheses.
    /// See https://github.com/dotnet/roslyn/blob/1a693dfd634d96d8226c14ead7992c4e24a2880f/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/CSharp/Extensions/ExpressionSyntaxExtensions.cs.
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
