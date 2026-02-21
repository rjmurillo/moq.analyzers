using Moq.Analyzers.Common.WellKnown;

namespace Moq.Analyzers.Test.Common;

public class EventSyntaxExtensionsTests
{
    [Fact]
    public void GetEventParameterTypes_ActionDelegate_ReturnsTypeArguments()
    {
        const string code = @"
using System;
class C
{
    event Action<int, string> MyEvent;
}";
        ITypeSymbol eventType = GetEventFieldType(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType);

        Assert.Equal(2, result.Length);
        Assert.Equal("int", result[0].ToDisplayString());
        Assert.Equal("string", result[1].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_ActionWithSingleTypeArg_ReturnsSingleType()
    {
        const string code = @"
using System;
class C
{
    event Action<double> MyEvent;
}";
        ITypeSymbol eventType = GetEventFieldType(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType);

        Assert.Single(result);
        Assert.Equal("double", result[0].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_EventHandlerGeneric_ReturnsSingleTypeArgument()
    {
        const string code = @"
using System;
class C
{
    event EventHandler<EventArgs> MyEvent;
}";
        ITypeSymbol eventType = GetEventFieldType(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType);

        Assert.Single(result);
        Assert.Equal("System.EventArgs", result[0].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_CustomDelegate_ReturnsInvokeMethodParameters()
    {
        const string code = @"
delegate void MyDelegate(int x, bool y);
class C
{
    event MyDelegate MyEvent;
}";
        ITypeSymbol eventType = GetEventFieldType(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType);

        Assert.Equal(2, result.Length);
        Assert.Equal("int", result[0].ToDisplayString());
        Assert.Equal("bool", result[1].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_CustomDelegateNoParameters_ReturnsEmpty()
    {
        const string code = @"
delegate void MyDelegate();
class C
{
    event MyDelegate MyEvent;
}";
        ITypeSymbol eventType = GetEventFieldType(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType);

        Assert.Empty(result);
    }

    [Fact]
    public void GetEventParameterTypes_NonNamedTypeSymbol_ReturnsEmpty()
    {
        const string code = @"
class C
{
    int[] Field;
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        VariableDeclaratorSyntax fieldSyntax = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        IFieldSymbol field = (IFieldSymbol)model.GetDeclaredSymbol(fieldSyntax)!;
        ITypeSymbol arrayType = field.Type;

        Assert.IsNotAssignableFrom<INamedTypeSymbol>(arrayType);

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(arrayType);

        Assert.Empty(result);
    }

    [Fact]
    public void GetEventParameterTypes_PlainEventHandler_ReturnsFallbackInvokeParams()
    {
        const string code = @"
using System;
class C
{
    event EventHandler MyEvent;
}";
        ITypeSymbol eventType = GetEventFieldType(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType);

        Assert.Equal(2, result.Length);
        Assert.Contains("object", result[0].ToDisplayString(), StringComparison.Ordinal);
        Assert.Contains("EventArgs", result[1].ToDisplayString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetEventParameterTypes_WithKnownSymbols_ActionDelegate_ReturnsTypeArguments()
    {
        const string code = @"
using System;
class C
{
    event Action<int, string> MyEvent;
}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Equal(2, result.Length);
        Assert.Equal("int", result[0].ToDisplayString());
        Assert.Equal("string", result[1].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_WithKnownSymbols_EventHandlerGeneric_ReturnsSingleTypeArgument()
    {
        const string code = @"
using System;
class C
{
    event EventHandler<EventArgs> MyEvent;
}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Single(result);
        Assert.Equal("System.EventArgs", result[0].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_WithKnownSymbols_CustomDelegate_ReturnsInvokeMethodParameters()
    {
        const string code = @"
delegate void MyDelegate(int x, bool y);
class C
{
    event MyDelegate MyEvent;
}";
        (ITypeSymbol eventType, KnownSymbols knownSymbols) = GetEventFieldTypeWithKnownSymbols(code, "MyEvent");

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(eventType, knownSymbols);

        Assert.Equal(2, result.Length);
        Assert.Equal("int", result[0].ToDisplayString());
        Assert.Equal("bool", result[1].ToDisplayString());
    }

    [Fact]
    public void GetEventParameterTypes_WithKnownSymbols_NonNamedTypeSymbol_ReturnsEmpty()
    {
        const string code = @"
class C
{
    int[] Field;
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        VariableDeclaratorSyntax fieldSyntax = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        IFieldSymbol field = (IFieldSymbol)model.GetDeclaredSymbol(fieldSyntax)!;

        ITypeSymbol[] result = EventSyntaxExtensions.GetEventParameterTypes(field.Type, knownSymbols);

        Assert.Empty(result);
    }

    [Fact]
    public void TryGetEventMethodArguments_NoArguments_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod();
    }
    void SomeMethod() {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, null));

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_ExtractorReturnsFalse_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod(42);
    }
    void SomeMethod(int x) {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (false, null));

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_ExtractorReturnsNullType_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod(42);
    }
    void SomeMethod(int x) {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, null));

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_OnlyEventSelector_ReturnsTrueWithEmptyArgs()
    {
        const string code = @"
using System;
delegate void MyDelegate(int x);
class C
{
    event MyDelegate MyEvent;
    void M()
    {
        SomeMethod(0);
    }
    void SomeMethod(int selector) {}
}";
        ITypeSymbol delegateType = GetEventFieldType(
            @"delegate void MyDelegate(int x);
class C { event MyDelegate MyEvent; }",
            "MyEvent");

        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, delegateType));

        Assert.True(result);
        Assert.Empty(eventArguments);
        Assert.Single(expectedParameterTypes);
        Assert.Equal("int", expectedParameterTypes[0].ToDisplayString());
    }

    [Fact]
    public void TryGetEventMethodArguments_WithAdditionalArgs_ReturnsTrueWithArgs()
    {
        const string code = @"
using System;
class C
{
    void M()
    {
        SomeMethod(0, 42, ""hello"");
    }
    void SomeMethod(int selector, int a, string b) {}
}";
        ITypeSymbol delegateType = GetEventFieldType(
            @"delegate void MyDelegate(int x, string y);
class C { event MyDelegate MyEvent; }",
            "MyEvent");

        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, delegateType));

        Assert.True(result);
        Assert.Equal(2, eventArguments.Length);
        Assert.Equal(2, expectedParameterTypes.Length);
    }

    [Fact]
    public void TryGetEventMethodArguments_WithKnownSymbols_NoArguments_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod();
    }
    void SomeMethod() {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, null),
            knownSymbols);

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_WithKnownSymbols_ExtractorReturnsFalse_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        SomeMethod(42);
    }
    void SomeMethod(int x) {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (false, null),
            knownSymbols);

        Assert.False(result);
        Assert.Empty(eventArguments);
        Assert.Empty(expectedParameterTypes);
    }

    [Fact]
    public void TryGetEventMethodArguments_WithKnownSymbols_ValidExtraction_ReturnsTrue()
    {
        const string code = @"
using System;
class C
{
    void M()
    {
        SomeMethod(0, 42);
    }
    void SomeMethod(int selector, int a) {}
}";
        ITypeSymbol delegateType = GetEventFieldType(
            @"
using System;
delegate void MyDelegate(int x);
class C { event MyDelegate MyEvent; }",
            "MyEvent");

        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            model,
            out ArgumentSyntax[] eventArguments,
            out ITypeSymbol[] expectedParameterTypes,
            (_, _) => (true, delegateType),
            knownSymbols);

        Assert.True(result);
        Assert.Single(eventArguments);
        Assert.Single(expectedParameterTypes);
    }

    // ValidateEventArgumentTypes requires SyntaxNodeAnalysisContext, which has no public
    // constructor and requires internal analyzer infrastructure to construct. These methods
    // are tested indirectly through RaiseEventArgumentsShouldMatchEventSignatureAnalyzerTests
    // and RaisesEventArgumentsShouldMatchEventSignatureAnalyzerTests, which exercise all
    // branching logic (too few args, too many args, wrong type, matching types, with/without
    // eventName).
#pragma warning disable ECS0900 // Boxing needed to cast to IEventSymbol from GetDeclaredSymbol
    private static ITypeSymbol GetEventFieldType(string code, string eventName)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        VariableDeclaratorSyntax variable = tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(v => v.Parent?.Parent is EventFieldDeclarationSyntax &&
                        string.Equals(v.Identifier.Text, eventName, StringComparison.Ordinal));
        IEventSymbol eventSymbol = (IEventSymbol)model.GetDeclaredSymbol(variable)!;
        return eventSymbol.Type;
    }

    private static (ITypeSymbol EventType, KnownSymbols KnownSymbols) GetEventFieldTypeWithKnownSymbols(
        string code,
        string eventName)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        KnownSymbols knownSymbols = new KnownSymbols(model.Compilation);
        VariableDeclaratorSyntax variable = tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(v => v.Parent?.Parent is EventFieldDeclarationSyntax &&
                        string.Equals(v.Identifier.Text, eventName, StringComparison.Ordinal));
        IEventSymbol eventSymbol = (IEventSymbol)model.GetDeclaredSymbol(variable)!;
        return (eventSymbol.Type, knownSymbols);
    }
#pragma warning restore ECS0900
}
