namespace Moq.Analyzers.Test.Common;

public class SyntaxNodeExtensionsTests
{
    [Fact]
    public void FindLocation_FindsMatchingNode()
    {
        // Arrange
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
        int y = x + 1;
    }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree });
        SemanticModel model = compilation.GetSemanticModel(tree);
        SyntaxNode root = tree.GetRoot();

        // Find the variable declaration for 'x'
        VariableDeclaratorSyntax? xDeclarator = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(v => string.Equals(v.Identifier.Text, "x", StringComparison.Ordinal));

        ISymbol? xSymbol = model.GetDeclaredSymbol(xDeclarator);
        Assert.NotNull(xSymbol);

        // Act - Look for an identifier reference to 'x' in the expression 'x + 1'
        Location? location = root.FindLocation<IdentifierNameSyntax>(xSymbol!, model);

        // Assert
        Assert.NotNull(location);
        Assert.True(location.IsInSource);
    }

    [Fact]
    public void FindLocation_ReturnsNull_WhenNodeNotFound()
    {
        // Arrange
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
    }
    
    void N()
    {
        int y = 1;
    }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree });
        SemanticModel model = compilation.GetSemanticModel(tree);
        SyntaxNode root = tree.GetRoot();

        // Get symbol for 'y' which is in method N
        VariableDeclaratorSyntax? yDeclarator = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(v => string.Equals(v.Identifier.Text, "y", StringComparison.Ordinal));

        ISymbol? ySymbol = model.GetDeclaredSymbol(yDeclarator);
        Assert.NotNull(ySymbol);

        // Act - Look for 'y' only within method M (where it doesn't exist)
        MethodDeclarationSyntax? methodM = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => string.Equals(m.Identifier.Text, "M", StringComparison.Ordinal));

        Location? location = methodM.FindLocation<IdentifierNameSyntax>(ySymbol!, model);

        // Assert
        Assert.Null(location);
    }

    [Fact]
    public void FindLocation_WithNullSemanticModel_ReturnsNull()
    {
        // Arrange
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
    }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree });
        SemanticModel model = compilation.GetSemanticModel(tree);
        SyntaxNode root = tree.GetRoot();

        VariableDeclaratorSyntax? xDeclarator = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(v => string.Equals(v.Identifier.Text, "x", StringComparison.Ordinal));

        ISymbol? xSymbol = model.GetDeclaredSymbol(xDeclarator);
        Assert.NotNull(xSymbol);

        // Act - Call with null semantic model
        Location? location = root.FindLocation<IdentifierNameSyntax>(xSymbol!, null);

        // Assert
        Assert.Null(location);
    }
}
