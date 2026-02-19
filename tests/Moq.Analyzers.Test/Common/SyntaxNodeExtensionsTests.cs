namespace Moq.Analyzers.Test.Common;

public class SyntaxNodeExtensionsTests
{
    [Fact]
    public void FindLocation_MatchingDescendantFound_ReturnsLocationWithCorrectSpan()
    {
        const string code = @"
class C
{
    void M()
    {
        var x = new C();
    }
    static C Create() => new C();
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        SyntaxNode root = tree.GetRoot();

        // Get the constructor symbol from the first ObjectCreationExpressionSyntax
        ObjectCreationExpressionSyntax creation = root
            .DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
        ISymbol? ctorSymbol = model.GetSymbolInfo(creation).Symbol;
        Assert.NotNull(ctorSymbol);

        // FindLocation should find a descendant ObjectCreationExpressionSyntax matching the ctor symbol
        Location? location = root.FindLocation<ObjectCreationExpressionSyntax>(ctorSymbol, model);

        Assert.NotNull(location);
        Assert.Equal(creation.Span, location.SourceSpan);
    }

    [Fact]
    public void FindLocation_NoMatchingDescendant_ReturnsNull()
    {
        const string code = @"
class C
{
    void M() { }
}
class D
{
    D() { }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        SyntaxNode root = tree.GetRoot();

        // Get the D constructor symbol
        ClassDeclarationSyntax classD = root
            .DescendantNodes().OfType<ClassDeclarationSyntax>()
            .First(c => string.Equals(c.Identifier.Text, "D", StringComparison.Ordinal));
        INamedTypeSymbol? typeDSymbol = model.GetDeclaredSymbol(classD);
        Assert.NotNull(typeDSymbol);
        IMethodSymbol ctorSymbol = typeDSymbol.InstanceConstructors.First();

        // Search within class C only (which has no ObjectCreationExpressionSyntax for D)
        ClassDeclarationSyntax classC = root
            .DescendantNodes().OfType<ClassDeclarationSyntax>()
            .First(c => string.Equals(c.Identifier.Text, "C", StringComparison.Ordinal));

        Location? location = classC.FindLocation<ObjectCreationExpressionSyntax>(ctorSymbol, model);

        Assert.Null(location);
    }

    [Fact]
    public void FindLocation_SemanticModelIsNull_ReturnsNull()
    {
        const string code = @"
class C
{
    void M()
    {
        var x = new C();
    }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        SyntaxNode root = tree.GetRoot();

        // Get a real symbol to pass in
        ObjectCreationExpressionSyntax creation = root
            .DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
        ISymbol? ctorSymbol = model.GetSymbolInfo(creation).Symbol;
        Assert.NotNull(ctorSymbol);

        // Call with null semantic model
        Location? location = root.FindLocation<ObjectCreationExpressionSyntax>(ctorSymbol, null);

        Assert.Null(location);
    }

    [Fact]
    public void WalkUpParentheses_NoParentheses_ReturnsSameExpression()
    {
        // In `1 + 2`, the LiteralExpressionSyntax "1" is not wrapped in parens
        SyntaxTree tree = CSharpSyntaxTree.ParseText(@"
class C
{
    int M() => 1 + 2;
}");
        SyntaxNode root = tree.GetRoot();
        LiteralExpressionSyntax literal = root
            .DescendantNodes().OfType<LiteralExpressionSyntax>()
            .First(l => string.Equals(l.Token.ValueText, "1", StringComparison.Ordinal));

        ExpressionSyntax? result = literal.WalkUpParentheses();

        Assert.Same(literal, result);
    }

    [Fact]
    public void WalkUpParentheses_OneLevelOfParentheses_ReturnsOutermostParen()
    {
        // In `var x = (1 + 2)`, the BinaryExpression is wrapped in one level of parens
        SyntaxTree tree = CSharpSyntaxTree.ParseText(@"
class C
{
    void M()
    {
        var x = (1 + 2);
    }
}");
        SyntaxNode root = tree.GetRoot();
        BinaryExpressionSyntax binary = root
            .DescendantNodes().OfType<BinaryExpressionSyntax>().First();
        ParenthesizedExpressionSyntax paren = root
            .DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();

        ExpressionSyntax? result = binary.WalkUpParentheses();

        Assert.Same(paren, result);
    }

    [Fact]
    public void WalkUpParentheses_MultipleLevelsOfParentheses_ReturnsOutermostParen()
    {
        // In `var x = ((1 + 2))`, the BinaryExpression is wrapped in two levels of parens
        SyntaxTree tree = CSharpSyntaxTree.ParseText(@"
class C
{
    void M()
    {
        var x = ((1 + 2));
    }
}");
        SyntaxNode root = tree.GetRoot();
        BinaryExpressionSyntax binary = root
            .DescendantNodes().OfType<BinaryExpressionSyntax>().First();
        ParenthesizedExpressionSyntax outerParen = root
            .DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();

        // Verify nested parentheses exist
        Assert.IsType<ParenthesizedExpressionSyntax>(binary.Parent);
        Assert.IsType<ParenthesizedExpressionSyntax>(binary.Parent!.Parent);

        ExpressionSyntax? result = binary.WalkUpParentheses();

        Assert.Same(outerParen, result);
    }

    [Fact]
    public void WalkUpParentheses_NullExpression_ReturnsNull()
    {
        ExpressionSyntax? expression = null;

        ExpressionSyntax? result = expression.WalkUpParentheses();

        Assert.Null(result);
    }

    [Fact]
    public void WalkDownParentheses_NoParentheses_ReturnsSameExpression()
    {
        // In `1 + 2`, the BinaryExpression has no surrounding parentheses
        SyntaxTree tree = CSharpSyntaxTree.ParseText(@"
class C
{
    int M() => 1 + 2;
}");
        SyntaxNode root = tree.GetRoot();
        BinaryExpressionSyntax binary = root
            .DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        ExpressionSyntax result = binary.WalkDownParentheses();

        Assert.Same(binary, result);
    }

    [Fact]
    public void WalkDownParentheses_OneLevelOfParentheses_ReturnsInnerExpression()
    {
        // In `var x = (1 + 2)`, the ParenthesizedExpression wraps BinaryExpression
        SyntaxTree tree = CSharpSyntaxTree.ParseText(@"
class C
{
    void M()
    {
        var x = (1 + 2);
    }
}");
        SyntaxNode root = tree.GetRoot();
        ParenthesizedExpressionSyntax paren = root
            .DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();
        BinaryExpressionSyntax inner = root
            .DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        ExpressionSyntax result = paren.WalkDownParentheses();

        Assert.Same(inner, result);
    }

    [Fact]
    public void WalkDownParentheses_MultipleLevelsOfParentheses_ReturnsInnermostExpression()
    {
        // In `var x = ((1 + 2))`, two levels of parens wrap BinaryExpression
        SyntaxTree tree = CSharpSyntaxTree.ParseText(@"
class C
{
    void M()
    {
        var x = ((1 + 2));
    }
}");
        SyntaxNode root = tree.GetRoot();
        ParenthesizedExpressionSyntax outerParen = root
            .DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();
        BinaryExpressionSyntax inner = root
            .DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        ExpressionSyntax result = outerParen.WalkDownParentheses();

        Assert.Same(inner, result);
    }
}
