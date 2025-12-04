namespace Moq.Analyzers.Test.Common;

public class NamedTypeSymbolExtensionsTests
{
    [Fact]
    public void IsEventHandlerDelegate_WithEventHandlerType_ReturnsTrue()
    {
        // Arrange
        const string code = @"
using System;

class C
{
    EventHandler<EventArgs> handler;
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(EventHandler<>).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        FieldDeclarationSyntax? field = tree.GetRoot()
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .First();

        INamedTypeSymbol? typeSymbol = model.GetTypeInfo(field.Declaration.Type).Type as INamedTypeSymbol;
        Assert.NotNull(typeSymbol);

        KnownSymbols knownSymbols = new(compilation);

        // Act
        bool result = typeSymbol!.IsEventHandlerDelegate(knownSymbols);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEventHandlerDelegate_WithNonEventHandlerType_ReturnsFalse()
    {
        // Arrange
        const string code = @"
using System;

class C
{
    Action<string> handler;
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Action<>).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        FieldDeclarationSyntax? field = tree.GetRoot()
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .First();

        INamedTypeSymbol? typeSymbol = model.GetTypeInfo(field.Declaration.Type).Type as INamedTypeSymbol;
        Assert.NotNull(typeSymbol);

        KnownSymbols knownSymbols = new(compilation);

        // Act
        bool result = typeSymbol!.IsEventHandlerDelegate(knownSymbols);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActionDelegate_WithActionType_ReturnsTrue()
    {
        // Arrange
        const string code = @"
using System;

class C
{
    Action action0;
    Action<string> action1;
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Action).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        List<FieldDeclarationSyntax> fields = tree.GetRoot()
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .ToList();

        KnownSymbols knownSymbols = new(compilation);

        // Act & Assert - Test Action
        INamedTypeSymbol? action0 = model.GetTypeInfo(fields[0].Declaration.Type).Type as INamedTypeSymbol;
        Assert.NotNull(action0);
        Assert.True(action0!.IsActionDelegate(knownSymbols));

        // Act & Assert - Test Action<T>
        INamedTypeSymbol? action1 = model.GetTypeInfo(fields[1].Declaration.Type).Type as INamedTypeSymbol;
        Assert.NotNull(action1);
        Assert.True(action1!.IsActionDelegate(knownSymbols));
    }

    [Fact]
    public void IsActionDelegate_WithNonActionType_ReturnsFalse()
    {
        // Arrange
        const string code = @"
using System;

class C
{
    Func<string> func;
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Func<>).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        FieldDeclarationSyntax? field = tree.GetRoot()
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .First();

        INamedTypeSymbol? typeSymbol = model.GetTypeInfo(field.Declaration.Type).Type as INamedTypeSymbol;
        Assert.NotNull(typeSymbol);

        KnownSymbols knownSymbols = new(compilation);

        // Act
        bool result = typeSymbol!.IsActionDelegate(knownSymbols);

        // Assert
        Assert.False(result);
    }
}
