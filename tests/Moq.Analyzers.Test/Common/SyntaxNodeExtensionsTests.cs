namespace Moq.Analyzers.Test.Common;

public class SyntaxNodeExtensionsTests
{
    [Fact]
    public void FindLocation_NoReferenceToSymbol_ReturnsNull()
    {
        // Arrange
        const string code = "class C { void M() {} }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        SyntaxNode root = tree.GetRoot();
        MethodDeclarationSyntax methodSyntax = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(methodSyntax);

        // Act
        Location? location = root.FindLocation<IdentifierNameSyntax>(methodSymbol!, model);

        // Assert - No IdentifierNameSyntax references M in a simple declaration
        Assert.Null(location);
    }

    [Fact]
    public void FindLocation_NoMatchingSymbol_ReturnsNull()
    {
        // Arrange
        const string code = "class C { void M() {} void N() {} }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        SyntaxNode root = tree.GetRoot();
        MethodDeclarationSyntax[] methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        IMethodSymbol? symbolN = model.GetDeclaredSymbol(methods[1]);

        // Act - search within method M's body for references to N
        Location? location = methods[0].FindLocation<IdentifierNameSyntax>(symbolN!, model);

        // Assert
        Assert.Null(location);
    }

    [Fact]
    public void FindLocation_WithMethodCall_ReturnsCallLocation()
    {
        // Arrange
        const string code = @"
class C
{
    void M() { N(); }
    void N() { }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        SyntaxNode root = tree.GetRoot();
        MethodDeclarationSyntax[] methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        IMethodSymbol? symbolN = model.GetDeclaredSymbol(methods[1]);

        // Act - search within method M for calls to N
        Location? location = methods[0].FindLocation<IdentifierNameSyntax>(symbolN!, model);

        // Assert
        Assert.NotNull(location);
        Assert.True(location.IsInSource);
    }

    [Fact]
    public void FindLocation_NullSemanticModel_ReturnsNull()
    {
        // Arrange
        const string code = "class C { void M() { } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        (SemanticModel model, _) = CompilationHelper.CreateCompilation(code);
        IMethodSymbol? symbol = model.GetDeclaredSymbol(
            model.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First());

        // Act
        Location? location = root.FindLocation<IdentifierNameSyntax>(symbol!, semanticModel: null);

        // Assert
        Assert.Null(location);
    }

    [Fact]
    public void WalkUpParentheses_NotWrapped_ReturnsSameExpression()
    {
        // Arrange
        const string code = "class C { int X = 1 + 2; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        BinaryExpressionSyntax binaryExpr = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        // Act
        ExpressionSyntax result = binaryExpr.WalkUpParentheses();

        // Assert
        Assert.Same(binaryExpr, result);
    }

    [Fact]
    public void WalkUpParentheses_SingleParentheses_ReturnsParenWrapper()
    {
        // Arrange
        const string code = "class C { int X = (1 + 2); }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        BinaryExpressionSyntax binaryExpr = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        // Act
        ExpressionSyntax result = binaryExpr.WalkUpParentheses();

        // Assert
        Assert.IsType<ParenthesizedExpressionSyntax>(result);
    }

    [Fact]
    public void WalkUpParentheses_DoubleParentheses_ReturnsOutermostWrapper()
    {
        // Arrange
        const string code = "class C { int X = ((1 + 2)); }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        BinaryExpressionSyntax binaryExpr = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        // Act
        ExpressionSyntax result = binaryExpr.WalkUpParentheses();

        // Assert
        Assert.IsType<ParenthesizedExpressionSyntax>(result);

        // The outermost parenthesized expression's parent should NOT be a ParenthesizedExpressionSyntax
        Assert.IsNotType<ParenthesizedExpressionSyntax>(result.Parent);
    }

    [Fact]
    public void WalkUpParentheses_NullExpression_ReturnsNull()
    {
        // Arrange
        ExpressionSyntax? nullExpr = null;

        // Act
        ExpressionSyntax? result = nullExpr.WalkUpParentheses();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void WalkDownParentheses_NotWrapped_ReturnsSameExpression()
    {
        // Arrange
        const string code = "class C { int X = 1 + 2; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        BinaryExpressionSyntax binaryExpr = root.DescendantNodes().OfType<BinaryExpressionSyntax>().First();

        // Act
        ExpressionSyntax result = binaryExpr.WalkDownParentheses();

        // Assert
        Assert.Same(binaryExpr, result);
    }

    [Fact]
    public void WalkDownParentheses_SingleParentheses_ReturnsInnerExpression()
    {
        // Arrange
        const string code = "class C { int X = (1 + 2); }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        ParenthesizedExpressionSyntax parenExpr = root.DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();

        // Act
        ExpressionSyntax result = parenExpr.WalkDownParentheses();

        // Assert
        Assert.IsType<BinaryExpressionSyntax>(result);
    }

    [Fact]
    public void WalkDownParentheses_DoubleParentheses_ReturnsInnermostExpression()
    {
        // Arrange
        const string code = "class C { int X = ((1 + 2)); }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        SyntaxNode root = tree.GetRoot();
        ParenthesizedExpressionSyntax outerParen = root.DescendantNodes().OfType<ParenthesizedExpressionSyntax>().First();

        // Act
        ExpressionSyntax result = outerParen.WalkDownParentheses();

        // Assert
        Assert.IsType<BinaryExpressionSyntax>(result);
    }

    [Fact]
    public void WalkDownParentheses_NullExpression_ReturnsNull()
    {
        // Arrange
        ExpressionSyntax? nullExpr = null;

        // Act
        ExpressionSyntax? result = nullExpr.WalkDownParentheses();

        // Assert
        Assert.Null(result);
    }
}
