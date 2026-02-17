namespace Moq.Analyzers.Test.Common;

public class SyntaxNodeExtensionsTests
{
    private static readonly MetadataReference CorlibReference;
    private static readonly MetadataReference SystemRuntimeReference;

#pragma warning disable S3963 // "static fields" should be initialized inline - conflicts with ECS1300
    static SyntaxNodeExtensionsTests()
    {
        CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll"));
    }
#pragma warning restore S3963

    private static MetadataReference[] CoreReferences => [CorlibReference, SystemRuntimeReference];

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
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
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
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
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
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
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
    public void GetParentSkippingParentheses_NoParenthesizedParent_ReturnsDirectParent()
    {
        // In `1 + 2`, the LiteralExpressionSyntax "1" has BinaryExpressionSyntax as parent
        SyntaxTree tree = CSharpSyntaxTree.ParseText(@"
class C
{
    int M() => 1 + 2;
}");
        SyntaxNode root = tree.GetRoot();
        LiteralExpressionSyntax literal = root
            .DescendantNodes().OfType<LiteralExpressionSyntax>()
            .First(l => string.Equals(l.Token.ValueText, "1", StringComparison.Ordinal));

        SyntaxNode? parent = literal.GetParentSkippingParentheses();

        Assert.NotNull(parent);
        Assert.IsType<BinaryExpressionSyntax>(parent);
        Assert.Same(literal.Parent, parent);
    }

    [Fact]
    public void GetParentSkippingParentheses_OneLevelOfParentheses_SkipsToGrandparent()
    {
        // In `var x = (1 + 2)`, the BinaryExpression `1 + 2` parent is ParenthesizedExpression
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

        // Direct parent should be ParenthesizedExpressionSyntax
        Assert.IsType<ParenthesizedExpressionSyntax>(binary.Parent);

        SyntaxNode? parent = binary.GetParentSkippingParentheses();

        Assert.NotNull(parent);
        Assert.IsNotType<ParenthesizedExpressionSyntax>(parent);
        Assert.IsType<EqualsValueClauseSyntax>(parent);
    }

    [Fact]
    public void GetParentSkippingParentheses_MultipleLevelsOfParentheses_SkipsAll()
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

        // Verify nested parentheses exist
        Assert.IsType<ParenthesizedExpressionSyntax>(binary.Parent);
        Assert.IsType<ParenthesizedExpressionSyntax>(binary.Parent!.Parent);

        SyntaxNode? parent = binary.GetParentSkippingParentheses();

        Assert.NotNull(parent);
        Assert.IsNotType<ParenthesizedExpressionSyntax>(parent);
        Assert.IsType<EqualsValueClauseSyntax>(parent);
    }

    [Fact]
    public void GetParentSkippingParentheses_NoParent_ReturnsNull()
    {
        // A CompilationUnit (root node) has no parent
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { }");
        SyntaxNode root = tree.GetRoot();

        SyntaxNode? parent = root.GetParentSkippingParentheses();

        Assert.Null(parent);
    }

    private static (SemanticModel Model, SyntaxTree Tree) CreateCompilation(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        return (model, tree);
    }
}
