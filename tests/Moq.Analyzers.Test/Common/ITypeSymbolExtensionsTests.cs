namespace Moq.Analyzers.Test.Common;

public class ITypeSymbolExtensionsTests
{
    [Fact]
    public void GetBaseTypesAndThis_ReturnsTypeAndBaseTypes()
    {
        // Arrange
        const string code = @"
class Base { }
class Derived : Base { }
class MostDerived : Derived { }
";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        ClassDeclarationSyntax? mostDerivedClass = tree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => string.Equals(c.Identifier.Text, "MostDerived", StringComparison.Ordinal));

        INamedTypeSymbol? typeSymbol = model.GetDeclaredSymbol(mostDerivedClass);
        Assert.NotNull(typeSymbol);

        // Act
        List<ITypeSymbol> baseTypes = typeSymbol!.GetBaseTypesAndThis().ToList();

        // Assert
        Assert.Equal(4, baseTypes.Count); // MostDerived, Derived, Base, object
        Assert.Equal("MostDerived", baseTypes[0].Name);
        Assert.Equal("Derived", baseTypes[1].Name);
        Assert.Equal("Base", baseTypes[2].Name);
        Assert.Equal("Object", baseTypes[3].Name);
    }

    [Fact]
    public void GetBaseTypesAndThis_WithNoBaseClass_ReturnsTypeAndObject()
    {
        // Arrange
        const string code = @"
class SimpleClass { }
";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        ClassDeclarationSyntax? simpleClass = tree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => string.Equals(c.Identifier.Text, "SimpleClass", StringComparison.Ordinal));

        INamedTypeSymbol? typeSymbol = model.GetDeclaredSymbol(simpleClass);
        Assert.NotNull(typeSymbol);

        // Act
        List<ITypeSymbol> baseTypes = typeSymbol!.GetBaseTypesAndThis().ToList();

        // Assert
        Assert.Equal(2, baseTypes.Count); // SimpleClass, object
        Assert.Equal("SimpleClass", baseTypes[0].Name);
        Assert.Equal("Object", baseTypes[1].Name);
    }

    [Fact]
    public void GetBaseTypesAndThis_WithInterface_ReturnsOnlyInterface()
    {
        // Arrange
        const string code = @"
interface IMyInterface { }
";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree });
        SemanticModel model = compilation.GetSemanticModel(tree);

        InterfaceDeclarationSyntax? interfaceDecl = tree.GetRoot()
            .DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>()
            .First();

        INamedTypeSymbol? typeSymbol = model.GetDeclaredSymbol(interfaceDecl);
        Assert.NotNull(typeSymbol);

        // Act
        List<ITypeSymbol> baseTypes = typeSymbol!.GetBaseTypesAndThis().ToList();

        // Assert
        Assert.Single(baseTypes); // Only the interface itself, no base type
        Assert.Equal("IMyInterface", baseTypes[0].Name);
    }
}
