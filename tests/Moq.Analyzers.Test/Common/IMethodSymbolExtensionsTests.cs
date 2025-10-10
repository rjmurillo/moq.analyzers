namespace Moq.Analyzers.Test.Common;

public class IMethodSymbolExtensionsTests
{
    [Fact]
    public void Overloads_ReturnsAllOverloadsExceptSelf()
    {
        // Arrange
        const string code = @"
class C
{
    void Method() { }
    void Method(int x) { }
    void Method(string x) { }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree });
        SemanticModel model = compilation.GetSemanticModel(tree);

        MethodDeclarationSyntax? firstMethod = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => string.Equals(m.Identifier.Text, "Method", StringComparison.Ordinal));

        IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(firstMethod);
        Assert.NotNull(methodSymbol);

        // Act
        List<IMethodSymbol> overloads = methodSymbol!.Overloads().ToList();

        // Assert
        Assert.Equal(2, overloads.Count);
        Assert.Contains(overloads, m => m.Parameters.Length == 1 && string.Equals(m.Parameters[0].Type.Name, "Int32", StringComparison.Ordinal));
        Assert.Contains(overloads, m => m.Parameters.Length == 1 && string.Equals(m.Parameters[0].Type.Name, "String", StringComparison.Ordinal));
        Assert.DoesNotContain(overloads, m => m.Parameters.Length == 0);
    }

    [Fact]
    public void Overloads_WithNullMethod_ReturnsEmpty()
    {
        // Act
        IEnumerable<IMethodSymbol> overloads = ((IMethodSymbol?)null).Overloads();

        // Assert
        Assert.Empty(overloads);
    }

    [Fact]
    public void TryGetOverloadWithParameterOfType_FindsMatchingOverload()
    {
        // Arrange
        const string code = @"
class C
{
    void Method() { }
    void Method(int x) { }
    void Method(string x) { }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        MethodDeclarationSyntax? firstMethod = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => string.Equals(m.Identifier.Text, "Method", StringComparison.Ordinal));

        IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(firstMethod);
        Assert.NotNull(methodSymbol);

        INamedTypeSymbol? stringType = compilation.GetSpecialType(SpecialType.System_String);
        Assert.NotNull(stringType);

        // Act
        bool result = methodSymbol!.TryGetOverloadWithParameterOfType(stringType!, out IMethodSymbol? methodMatch, out IParameterSymbol? parameterMatch);

        // Assert
        Assert.True(result);
        Assert.NotNull(methodMatch);
        Assert.NotNull(parameterMatch);
#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
        Assert.Single(methodMatch!.Parameters);
#pragma warning restore ECS0900
        Assert.Equal("String", parameterMatch!.Type.Name);
    }

    [Fact]
    public void TryGetOverloadWithParameterOfType_NoMatch_ReturnsFalse()
    {
        // Arrange
        const string code = @"
class C
{
    void Method() { }
    void Method(int x) { }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        MethodDeclarationSyntax? firstMethod = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => string.Equals(m.Identifier.Text, "Method", StringComparison.Ordinal));

        IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(firstMethod);
        Assert.NotNull(methodSymbol);

        INamedTypeSymbol? stringType = compilation.GetSpecialType(SpecialType.System_String);
        Assert.NotNull(stringType);

        // Act
        bool result = methodSymbol!.TryGetOverloadWithParameterOfType(stringType!, out IMethodSymbol? methodMatch, out IParameterSymbol? parameterMatch);

        // Assert
        Assert.False(result);
        Assert.Null(methodMatch);
        Assert.Null(parameterMatch);
    }

    [Fact]
    public void TryGetParameterOfType_FindsMatchingParameter()
    {
        // Arrange
        const string code = @"
class C
{
    void Method(int x, string y, double z) { }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        MethodDeclarationSyntax? method = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();

        IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(method);
        Assert.NotNull(methodSymbol);

        INamedTypeSymbol? stringType = compilation.GetSpecialType(SpecialType.System_String);
        Assert.NotNull(stringType);

        // Act
        bool result = methodSymbol!.TryGetParameterOfType(stringType!, out IParameterSymbol? match);

        // Assert
        Assert.True(result);
        Assert.NotNull(match);
        Assert.Equal("y", match!.Name);
        Assert.Equal("String", match.Type.Name);
    }

    [Fact]
    public void TryGetParameterOfType_NoMatch_ReturnsFalse()
    {
        // Arrange
        const string code = @"
class C
{
    void Method(int x, double y) { }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        MethodDeclarationSyntax? method = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();

        IMethodSymbol? methodSymbol = model.GetDeclaredSymbol(method);
        Assert.NotNull(methodSymbol);

        INamedTypeSymbol? stringType = compilation.GetSpecialType(SpecialType.System_String);
        Assert.NotNull(stringType);

        // Act
        bool result = methodSymbol!.TryGetParameterOfType(stringType!, out IParameterSymbol? match);

        // Assert
        Assert.False(result);
        Assert.Null(match);
    }
}
