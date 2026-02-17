namespace Moq.Analyzers.Test.Common;

public class ITypeSymbolExtensionsTests
{
    private static readonly MetadataReference CorlibReference;
    private static readonly MetadataReference SystemRuntimeReference;

#pragma warning disable S3963 // "static fields" should be initialized inline - conflicts with ECS1300
    static ITypeSymbolExtensionsTests()
    {
        CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll"));
    }
#pragma warning restore S3963

    private static MetadataReference[] CoreReferences =>
        [CorlibReference, SystemRuntimeReference];

    [Fact]
    public void GetBaseTypesAndThis_ClassWithNoExplicitBase_ReturnsClassAndObject()
    {
        INamedTypeSymbol classSymbol = GetNamedTypeSymbol("public class MyClass { }", "MyClass");

        List<ITypeSymbol> result = classSymbol.GetBaseTypesAndThis().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("MyClass", result[0].Name);
        Assert.Equal("Object", result[1].Name);
    }

    [Fact]
    public void GetBaseTypesAndThis_ClassWithInheritanceChain_ReturnsAllTypesInOrder()
    {
        string code = @"
public class GrandParent { }
public class Parent : GrandParent { }
public class Child : Parent { }";
        INamedTypeSymbol classSymbol = GetNamedTypeSymbol(code, "Child");

        List<ITypeSymbol> result = classSymbol.GetBaseTypesAndThis().ToList();

        Assert.Equal(4, result.Count);
        Assert.Equal("Child", result[0].Name);
        Assert.Equal("Parent", result[1].Name);
        Assert.Equal("GrandParent", result[2].Name);
        Assert.Equal("Object", result[3].Name);
    }

    [Fact]
    public void GetBaseTypesAndThis_Interface_ReturnsOnlyInterface()
    {
        INamedTypeSymbol interfaceSymbol = GetNamedTypeSymbol("public interface IMyInterface { }", "IMyInterface");

        List<ITypeSymbol> result = interfaceSymbol.GetBaseTypesAndThis().ToList();

        Assert.Single(result);
        Assert.Equal("IMyInterface", result[0].Name);
    }

    private static INamedTypeSymbol GetNamedTypeSymbol(string code, string typeName)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Select(t => model.GetDeclaredSymbol(t))
            .OfType<INamedTypeSymbol>()
            .First(t => string.Equals(t.Name, typeName, StringComparison.Ordinal));
    }
}
