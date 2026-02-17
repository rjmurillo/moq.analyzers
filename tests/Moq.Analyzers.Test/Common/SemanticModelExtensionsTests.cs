using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

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

    [Fact]
    public async Task FindSetupMethodFromCallbackInvocation_ValidReturnsChain_FindsSetupInvocation()
    {
        const string code = @"
using Moq;
public interface IFoo { int Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar()).Returns(42);
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax returnsInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Returns", StringComparison.Ordinal));

        InvocationExpressionSyntax? setupInvocation = model.FindSetupMethodFromCallbackInvocation(
            knownSymbols, returnsInvocation, CancellationToken.None);

        Assert.NotNull(setupInvocation);
        MemberAccessExpressionSyntax setupAccess = (MemberAccessExpressionSyntax)setupInvocation!.Expression;
        Assert.Equal("Setup", setupAccess.Name.Identifier.Text);
    }

    [Fact]
    public async Task FindSetupMethodFromCallbackInvocation_ExpressionNotInvocation_ReturnsNull()
    {
        const string code = @"
using Moq;
public interface IFoo { void Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar());
    }
}";
        (SemanticModel model, _) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);

        // Pass a non-invocation expression (a literal)
        LiteralExpressionSyntax literal = SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(42));

        InvocationExpressionSyntax? result = model.FindSetupMethodFromCallbackInvocation(
            knownSymbols, literal, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindSetupMethodFromCallbackInvocation_ReturnsChain_FindsSetup()
    {
        const string code = @"
using Moq;
public interface IFoo { int Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar()).Returns(42);
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax returnsInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Returns", StringComparison.Ordinal));

        InvocationExpressionSyntax? setupInvocation = model.FindSetupMethodFromCallbackInvocation(
            knownSymbols, returnsInvocation, CancellationToken.None);

        Assert.NotNull(setupInvocation);
    }

    [Fact]
    public async Task GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation_ValidSetup_ReturnsMethodSymbols()
    {
        const string code = @"
using Moq;
public interface IFoo { int Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar()).Returns(42);
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax setupInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Setup", StringComparison.Ordinal));

        IEnumerable<IMethodSymbol> symbols = model.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(setupInvocation);

        Assert.NotEmpty(symbols);
        Assert.Contains(symbols, s => string.Equals(s.Name, "Bar", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation_NullInput_ReturnsEmpty()
    {
        const string code = @"
using Moq;
public class C { }";
        (SemanticModel model, _) = await CreateMoqCompilationAsync(code);

        IEnumerable<IMethodSymbol> symbols = model.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(null);

        Assert.Empty(symbols);
    }

    [Fact]
    public async Task GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation_LambdaBodyNotInvocation_ReturnsEmpty()
    {
        // Create a setup where the lambda body is a property access, not a method invocation
        const string code = @"
using Moq;
public interface IFoo { int Value { get; } }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Value);
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax setupInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Setup", StringComparison.Ordinal));

        IEnumerable<IMethodSymbol> symbols = model.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(setupInvocation);

        Assert.Empty(symbols);
    }

    [Fact]
    public async Task IsCallbackOrReturnInvocation_CallbackInvocation_ReturnsTrue()
    {
        const string code = @"
using Moq;
public interface IFoo { void Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar()).Callback(() => { });
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax callbackInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Callback", StringComparison.Ordinal));

        bool result = model.IsCallbackOrReturnInvocation(callbackInvocation, knownSymbols);

        Assert.True(result);
    }

    [Fact]
    public async Task IsCallbackOrReturnInvocation_ReturnsInvocation_ReturnsTrue()
    {
        const string code = @"
using Moq;
public interface IFoo { int Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar()).Returns(42);
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax returnsInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Returns", StringComparison.Ordinal));

        bool result = model.IsCallbackOrReturnInvocation(returnsInvocation, knownSymbols);

        Assert.True(result);
    }

    [Fact]
    public async Task IsCallbackOrReturnInvocation_NonCallbackReturnsMethodName_ReturnsFalse()
    {
        const string code = @"
using Moq;
public interface IFoo { int Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar());
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax setupInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Setup", StringComparison.Ordinal));

        bool result = model.IsCallbackOrReturnInvocation(setupInvocation, knownSymbols);

        Assert.False(result);
    }

    [Fact]
    public async Task IsCallbackOrReturnInvocation_ExpressionNotMemberAccess_ReturnsFalse()
    {
        // Create an invocation that does not use member access (direct method call)
        const string code = @"
using Moq;
public class C
{
    static void Foo() { }
    public void M()
    {
        Foo();
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax fooInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is IdentifierNameSyntax id
                && string.Equals(id.Identifier.Text, "Foo", StringComparison.Ordinal));

        bool result = model.IsCallbackOrReturnInvocation(fooInvocation, knownSymbols);

        Assert.False(result);
    }

    [Fact]
    public async Task IsRaisesInvocation_RaisesCall_ReturnsTrue()
    {
        const string code = @"
using Moq;
using System;
public interface IFoo { event EventHandler MyEvent; void Bar(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Bar()).Raises(x => x.MyEvent += null, EventArgs.Empty);
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax raisesInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Raises", StringComparison.Ordinal));

        bool result = model.IsRaisesInvocation(raisesInvocation, knownSymbols);

        Assert.True(result);
    }

    [Fact]
    public async Task IsRaisesInvocation_ExpressionNotMemberAccess_ReturnsFalse()
    {
        const string code = @"
using Moq;
public class C
{
    static void Raises() { }
    public void M()
    {
        Raises();
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax raisesInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is IdentifierNameSyntax id
                && string.Equals(id.Identifier.Text, "Raises", StringComparison.Ordinal));

        bool result = model.IsRaisesInvocation(raisesInvocation, knownSymbols);

        Assert.False(result);
    }

    [Fact]
    public async Task IsRaisesInvocation_NonMoqMethodNamedRaises_ReturnsFalse()
    {
        const string code = @"
using Moq;
public class MyClass
{
    public MyClass Raises() => this;
}
public class C
{
    public void M()
    {
        var obj = new MyClass();
        obj.Raises();
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CreateMoqCompilationAsync(code);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InvocationExpressionSyntax raisesInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                && string.Equals(ma.Name.Identifier.Text, "Raises", StringComparison.Ordinal));

        bool result = model.IsRaisesInvocation(raisesInvocation, knownSymbols);

        Assert.False(result);
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

    private static async Task<(SemanticModel Model, SyntaxTree Tree)> CreateMoqCompilationAsync(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        MetadataReference[] references = await GetMoqReferencesAsync().ConfigureAwait(false);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        return (model, tree);
    }

    private static async Task<MetadataReference[]> GetMoqReferencesAsync()
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[ReferenceAssemblyCatalog.Net80WithNewMoq];
        ImmutableArray<MetadataReference> resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None).ConfigureAwait(false);
        return [.. resolved];
    }
}
