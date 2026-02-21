using Moq.Analyzers.Common.WellKnown;

namespace Moq.Analyzers.Test.Common;

public class NamedTypeSymbolExtensionsTests
{
    [Fact]
    public void IsEventHandlerDelegate_GenericEventHandler_ReturnsTrue()
    {
        (INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols) =
            GetFieldTypeAndKnownSymbols("using System; class C { EventHandler<EventArgs> handler; }");

        bool result = typeSymbol.IsEventHandlerDelegate(knownSymbols);

        Assert.True(result);
    }

    [Fact]
    public void IsEventHandlerDelegate_ActionType_ReturnsFalse()
    {
        (INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols) =
            GetFieldTypeAndKnownSymbols("using System; class C { Action<string> handler; }");

        bool result = typeSymbol.IsEventHandlerDelegate(knownSymbols);

        Assert.False(result);
    }

    [Fact]
    public void IsEventHandlerDelegate_FuncType_ReturnsFalse()
    {
        (INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols) =
            GetFieldTypeAndKnownSymbols("using System; class C { Func<string> handler; }");

        bool result = typeSymbol.IsEventHandlerDelegate(knownSymbols);

        Assert.False(result);
    }

    [Fact]
    public void IsActionDelegate_NonGenericAction_ReturnsTrue()
    {
        (INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols) =
            GetFieldTypeAndKnownSymbols("using System; class C { Action action; }");

        bool result = typeSymbol.IsActionDelegate(knownSymbols);

        Assert.True(result);
    }

    [Fact]
    public void IsActionDelegate_GenericAction_ReturnsTrue()
    {
        (INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols) =
            GetFieldTypeAndKnownSymbols("using System; class C { Action<string> action; }");

        bool result = typeSymbol.IsActionDelegate(knownSymbols);

        Assert.True(result);
    }

    [Fact]
    public void IsActionDelegate_FuncType_ReturnsFalse()
    {
        (INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols) =
            GetFieldTypeAndKnownSymbols("using System; class C { Func<string> func; }");

        bool result = typeSymbol.IsActionDelegate(knownSymbols);

        Assert.False(result);
    }

    [Fact]
    public void IsActionDelegate_EventHandlerType_ReturnsFalse()
    {
        (INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols) =
            GetFieldTypeAndKnownSymbols("using System; class C { EventHandler<EventArgs> handler; }");

        bool result = typeSymbol.IsActionDelegate(knownSymbols);

        Assert.False(result);
    }

    private static (INamedTypeSymbol TypeSymbol, KnownSymbols KnownSymbols) GetFieldTypeAndKnownSymbols(string code)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);

        FieldDeclarationSyntax field = tree.GetRoot()
            .DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .First();

        INamedTypeSymbol typeSymbol = (INamedTypeSymbol)model.GetTypeInfo(field.Declaration.Type).Type!;
        KnownSymbols knownSymbols = new(model.Compilation);

        return (typeSymbol, knownSymbols);
    }
}
