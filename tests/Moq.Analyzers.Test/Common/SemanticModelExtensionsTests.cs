using Microsoft.CodeAnalysis.CSharp;

namespace Moq.Analyzers.Test.Common;

public class SemanticModelExtensionsTests
{
    // Methods requiring Moq compilation reference (FindSetupMethodFromCallbackInvocation,
    // GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation, IsCallbackOrReturnInvocation,
    // IsRaisesInvocation) require Moq symbols and are covered by integration tests.
    private static readonly MetadataReference CorlibReference;
    private static readonly MetadataReference SystemRuntimeReference;
    private static readonly MetadataReference SystemLinqReference;
    private static readonly MetadataReference SystemLinqExpressionsReference;

#pragma warning disable S3963 // "static fields" should be initialized inline - conflicts with ECS1300
    static SemanticModelExtensionsTests()
    {
        CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll"));
        SystemLinqReference = MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location);
        SystemLinqExpressionsReference = MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location);
    }
#pragma warning restore S3963

    private static MetadataReference[] CoreReferences =>
        [CorlibReference, SystemRuntimeReference, SystemLinqReference, SystemLinqExpressionsReference];

    [Fact]
    public void HasConversion_IntToLong_ImplicitConversion_ReturnsTrue()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 1;
        long y = x;
    }
}";
        (SemanticModel model, ITypeSymbol intType, ITypeSymbol longType) = GetTwoVariableTypes(code, "x", "y");

        bool result = model.HasConversion(intType, longType);

        Assert.True(result);
    }

    [Fact]
    public void HasConversion_LongToInt_ExplicitConversion_ReturnsTrue()
    {
        const string code = @"
class C
{
    void M()
    {
        long x = 1;
        int y = 0;
    }
}";
        (SemanticModel model, ITypeSymbol longType, ITypeSymbol intType) = GetTwoVariableTypes(code, "x", "y");

        bool result = model.HasConversion(longType, intType);

        Assert.True(result);
    }

    [Fact]
    public void HasConversion_IntToInt_IdentityConversion_ReturnsTrue()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 1;
        int y = 2;
    }
}";
        (SemanticModel model, ITypeSymbol intType1, ITypeSymbol intType2) = GetTwoVariableTypes(code, "x", "y");

        bool result = model.HasConversion(intType1, intType2);

        Assert.True(result);
    }

    [Fact]
    public void HasConversion_StringToInt_NoConversion_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        string x = ""hello"";
        int y = 0;
    }
}";
        (SemanticModel model, ITypeSymbol stringType, ITypeSymbol intType) = GetTwoVariableTypes(code, "x", "y");

        bool result = model.HasConversion(stringType, intType);

        Assert.False(result);
    }

    [Fact]
    public void HasConversion_DerivedToBase_ImplicitReferenceConversion_ReturnsTrue()
    {
        const string code = @"
class Base { }
class Derived : Base { }
class C
{
    void M()
    {
        Derived d = new Derived();
        Base b = d;
    }
}";
        (SemanticModel model, ITypeSymbol derivedType, ITypeSymbol baseType) = GetTwoVariableTypes(code, "d", "b");

        bool result = model.HasConversion(derivedType, baseType);

        Assert.True(result);
    }

    [Fact]
    public void HasConversion_BaseToDerived_ExplicitConversion_ReturnsTrue()
    {
        const string code = @"
class Base { }
class Derived : Base { }
class C
{
    void M()
    {
        Base b = new Base();
        Derived d = null;
    }
}";
        (SemanticModel model, ITypeSymbol baseType, ITypeSymbol derivedType) = GetTwoVariableTypes(code, "b", "d");

        bool result = model.HasConversion(baseType, derivedType);

        Assert.True(result);
    }

    [Fact]
    public void HasConversion_UnrelatedClasses_ReturnsFalse()
    {
        const string code = @"
class A { }
class B { }
class C
{
    void M()
    {
        A a = new A();
        B b = new B();
    }
}";
        (SemanticModel model, ITypeSymbol typeA, ITypeSymbol typeB) = GetTwoVariableTypes(code, "a", "b");

        bool result = model.HasConversion(typeA, typeB);

        Assert.False(result);
    }

    [Fact]
    public void HasConversion_IntToDouble_ImplicitNumericConversion_ReturnsTrue()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 1;
        double y = 0.0;
    }
}";
        (SemanticModel model, ITypeSymbol intType, ITypeSymbol doubleType) = GetTwoVariableTypes(code, "x", "y");

        bool result = model.HasConversion(intType, doubleType);

        Assert.True(result);
    }

    [Fact]
    public void HasConversion_ClassToInterface_ImplicitConversion_ReturnsTrue()
    {
        const string code = @"
using System;
class C
{
    void M()
    {
        string s = ""hello"";
        IComparable c = s;
    }
}";
        (SemanticModel model, ITypeSymbol stringType, ITypeSymbol interfaceType) = GetTwoVariableTypes(code, "s", "c");

        bool result = model.HasConversion(stringType, interfaceType);

        Assert.True(result);
    }

    [Fact]
    public void TryGetEventNameFromLambdaSelector_ValidEventLambda_ReturnsTrueWithEventName()
    {
        const string code = @"
using System;
using System.Linq.Expressions;
class C
{
    event EventHandler MyEvent;
    void M()
    {
        Expression<Action<C>> selector = p => p.MyEvent += null;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventNameFromLambdaSelector(lambda, out string? eventName);

        Assert.True(result);
        Assert.Contains("MyEvent", eventName, StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetEventNameFromLambdaSelector_NonLambdaExpression_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);

        LiteralExpressionSyntax literal = tree.GetRoot()
            .DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .First();

        bool result = model.TryGetEventNameFromLambdaSelector(literal, out string? eventName);

        Assert.False(result);
        Assert.Null(eventName);
    }

    [Fact]
    public void TryGetEventNameFromLambdaSelector_LambdaWithoutAssignment_ReturnsFalse()
    {
        const string code = @"
using System;
using System.Linq.Expressions;
class C
{
    int Value;
    void M()
    {
        Expression<Func<C, int>> selector = p => p.Value;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventNameFromLambdaSelector(lambda, out string? eventName);

        Assert.False(result);
        Assert.Null(eventName);
    }

    [Fact]
    public void TryGetEventNameFromLambdaSelector_LambdaWithMinusEqualsOperator_ReturnsFalse()
    {
        const string code = @"
using System;
using System.Linq.Expressions;
class C
{
    event EventHandler MyEvent;
    void M()
    {
        Expression<Action<C>> selector = p => p.MyEvent -= null;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventNameFromLambdaSelector(lambda, out string? eventName);

        Assert.False(result);
        Assert.Null(eventName);
    }

    [Fact]
    public void TryGetEventNameFromLambdaSelector_LambdaLeftSideNotMemberAccess_ReturnsFalse()
    {
        const string code = @"
using System;
class C
{
    void M()
    {
        int x = 0;
        Func<int> selector = () => x += 1;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventNameFromLambdaSelector(lambda, out string? eventName);

        Assert.False(result);
        Assert.Null(eventName);
    }

    [Fact]
    public void TryGetEventNameFromLambdaSelector_LambdaPlusEqualsOnNonEvent_ReturnsFalse()
    {
        const string code = @"
using System;
class C
{
    public int Value;
    void M()
    {
        Func<C, int> selector = p => p.Value += 1;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventNameFromLambdaSelector(lambda, out string? eventName);

        Assert.False(result);
        Assert.Null(eventName);
    }

    [Fact]
    public void TryGetEventTypeFromLambdaSelector_ValidEventLambda_ReturnsTrueWithEventType()
    {
        const string code = @"
using System;
using System.Linq.Expressions;
class C
{
    event EventHandler MyEvent;
    void M()
    {
        Expression<Action<C>> selector = p => p.MyEvent += null;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventTypeFromLambdaSelector(lambda, out ITypeSymbol? eventType);

        Assert.True(result);
        Assert.NotNull(eventType);
        Assert.Contains("EventHandler", eventType.ToDisplayString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetEventTypeFromLambdaSelector_NonLambdaExpression_ReturnsFalse()
    {
        const string code = @"
class C
{
    void M()
    {
        int x = 42;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);

        LiteralExpressionSyntax literal = tree.GetRoot()
            .DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .First();

        bool result = model.TryGetEventTypeFromLambdaSelector(literal, out ITypeSymbol? eventType);

        Assert.False(result);
        Assert.Null(eventType);
    }

    [Fact]
    public void TryGetEventTypeFromLambdaSelector_LambdaWithMinusEquals_ReturnsFalse()
    {
        const string code = @"
using System;
using System.Linq.Expressions;
class C
{
    event EventHandler MyEvent;
    void M()
    {
        Expression<Action<C>> selector = p => p.MyEvent -= null;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventTypeFromLambdaSelector(lambda, out ITypeSymbol? eventType);

        Assert.False(result);
        Assert.Null(eventType);
    }

    [Fact]
    public void TryGetEventTypeFromLambdaSelector_CustomEventType_ReturnsTrueWithCorrectType()
    {
        const string code = @"
using System;
using System.Linq.Expressions;
class MyEventArgs : EventArgs { }
delegate void MyEventHandler(object sender, MyEventArgs e);
class C
{
    event MyEventHandler MyEvent;
    void M()
    {
        Expression<Action<C>> selector = p => p.MyEvent += null;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventTypeFromLambdaSelector(lambda, out ITypeSymbol? eventType);

        Assert.True(result);
        Assert.NotNull(eventType);
        Assert.Contains("MyEventHandler", eventType.ToDisplayString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetEventTypeFromLambdaSelector_LambdaWithoutAssignment_ReturnsFalse()
    {
        const string code = @"
using System;
using System.Linq.Expressions;
class C
{
    int Value;
    void M()
    {
        Expression<Func<C, int>> selector = p => p.Value;
    }
}";
        (SemanticModel model, ExpressionSyntax lambda) = GetLambdaFromVariableInitializer(code, "selector");

        bool result = model.TryGetEventTypeFromLambdaSelector(lambda, out ITypeSymbol? eventType);

        Assert.False(result);
        Assert.Null(eventType);
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

    private static (SemanticModel Model, ITypeSymbol FirstType, ITypeSymbol SecondType) GetTwoVariableTypes(
        string code,
        string firstName,
        string secondName)
    {
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        VariableDeclaratorSyntax[] declarators = tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .ToArray();

        ILocalSymbol firstSymbol = (ILocalSymbol)model.GetDeclaredSymbol(
            declarators.First(d => string.Equals(d.Identifier.Text, firstName, StringComparison.Ordinal)))!;
        ILocalSymbol secondSymbol = (ILocalSymbol)model.GetDeclaredSymbol(
            declarators.First(d => string.Equals(d.Identifier.Text, secondName, StringComparison.Ordinal)))!;

        return (model, firstSymbol.Type, secondSymbol.Type);
    }

    private static (SemanticModel Model, ExpressionSyntax Lambda) GetLambdaFromVariableInitializer(
        string code,
        string variableName)
    {
        (SemanticModel model, SyntaxTree tree) = CreateCompilation(code);
        VariableDeclaratorSyntax declarator = tree.GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .First(d => string.Equals(d.Identifier.Text, variableName, StringComparison.Ordinal));

        ExpressionSyntax lambda = declarator.Initializer!.Value;
        return (model, lambda);
    }
}
