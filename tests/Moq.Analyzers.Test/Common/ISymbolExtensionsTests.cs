using System.Collections.Immutable;
using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public class ISymbolExtensionsTests
{
#pragma warning disable ECS1300 // Static field init is simpler than static constructor for single field
    private static readonly MetadataReference SystemThreadingTasksReference =
        MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location);
#pragma warning restore ECS1300

    private static MetadataReference[] CoreReferencesWithTasks =>
        [CompilationHelper.CorlibReference, CompilationHelper.SystemRuntimeReference, SystemThreadingTasksReference, CompilationHelper.SystemLinqReference];

    [Fact]
    public void IsConstructor_PublicConstructor_ReturnsTrue()
    {
        IMethodSymbol ctor = GetConstructor("public class C { public C() {} }", Accessibility.Public);
        Assert.True(ctor.IsConstructor());
    }

    [Fact]
    public void IsConstructor_InternalConstructor_ReturnsTrue()
    {
        IMethodSymbol ctor = GetConstructor("public class C { internal C() {} }", Accessibility.Internal);
        Assert.True(ctor.IsConstructor());
    }

    [Fact]
    public void IsConstructor_ProtectedConstructor_ReturnsTrue()
    {
        IMethodSymbol ctor = GetConstructor("public class C { protected C() {} }", Accessibility.Protected);
        Assert.True(ctor.IsConstructor());
    }

    [Fact]
    public void IsConstructor_PrivateConstructor_ReturnsFalse()
    {
        IMethodSymbol ctor = GetConstructor("public class C { private C() {} }", Accessibility.Private);
        Assert.False(ctor.IsConstructor());
    }

    [Fact]
    public void IsConstructor_StaticConstructor_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation("public class C { static C() {} }");
        IMethodSymbol staticCtor = GetMethodSymbols(model, tree)
            .First(m => m.MethodKind == MethodKind.StaticConstructor);
        Assert.False(((ISymbol)staticCtor).IsConstructor());
    }

    [Fact]
    public void IsConstructor_RegularMethod_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation("public class C { public void M() {} }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        Assert.False(((ISymbol)method).IsConstructor());
    }

    [Fact]
    public void IsConstructor_Property_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation("public class C { public int P { get; set; } }");
        IPropertySymbol prop = tree.GetRoot()
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => model.GetDeclaredSymbol(p))
            .OfType<IPropertySymbol>()
            .First();
        Assert.False(((ISymbol)prop).IsConstructor());
    }

    [Fact]
    public void IsInstanceOf_NullSymbol_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation("public class C { public void M() {} }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        ISymbol? nullSymbol = null;
        Assert.False(nullSymbol.IsInstanceOf(method));
    }

    [Fact]
    public void IsInstanceOf_NullOther_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation("public class C { public void M() {} }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        Assert.False(method.IsInstanceOf<IMethodSymbol>(null));
    }

    [Fact]
    public void IsInstanceOf_MethodMatchingOriginalDefinition_ReturnsTrue()
    {
        string code = @"
public class C
{
    public void M<T>(T value) {}
    public void Caller() { M<int>(42); }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        IMethodSymbol? calledMethod = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        IMethodSymbol originalDefinition = calledMethod!.OriginalDefinition;

        Assert.True(((ISymbol)calledMethod).IsInstanceOf(originalDefinition));
    }

    [Fact]
    public void IsInstanceOf_MethodNotMatching_ReturnsFalse()
    {
        string code = @"
public class C
{
    public void M1() {}
    public void M2() {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol[] methods = GetMethodSymbols(model, tree)
            .Where(m => m.Name.StartsWith("M", StringComparison.Ordinal))
            .ToArray();
        Assert.False(((ISymbol)methods[0]).IsInstanceOf(methods[1]));
    }

    [Fact]
    public void IsInstanceOf_MethodWithReducedFrom_ReturnsTrue()
    {
        string code = @"
public static class Ext
{
    public static void DoSomething(this string s) {}
}
public class C
{
    public void Caller() { ""hello"".DoSomething(); }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        IMethodSymbol? reducedMethod = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        Assert.NotNull(reducedMethod);
        Assert.NotNull(reducedMethod.ReducedFrom);

        IMethodSymbol staticDefinition = reducedMethod.ReducedFrom;
        Assert.True(((ISymbol)reducedMethod).IsInstanceOf(staticDefinition));
    }

    [Fact]
    public void IsInstanceOf_ParameterMatchingOrdinalAndDefinition_ReturnsTrue()
    {
        string code = @"
public class C
{
    public void M<T>(T value) {}
    public void Caller() { M<int>(42); }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        IMethodSymbol? calledMethod = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        IParameterSymbol constructedParam = calledMethod!.Parameters[0];
        IParameterSymbol originalParam = calledMethod.OriginalDefinition.Parameters[0];

        Assert.True(((ISymbol)constructedParam).IsInstanceOf(originalParam));
    }

    [Fact]
    public void IsInstanceOf_ParameterNotMatching_ReturnsFalse()
    {
        string code = @"
public class C
{
    public void M(int a, string b) {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        IParameterSymbol paramA = method.Parameters[0];
        IParameterSymbol paramB = method.Parameters[1];

        Assert.False(((ISymbol)paramA).IsInstanceOf(paramB));
    }

    [Fact]
    public void IsInstanceOf_GenericNamedType_ReturnsTrue()
    {
        string code = @"
using System.Collections.Generic;
public class C
{
    public List<int> Prop { get; set; }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        PropertyDeclarationSyntax propSyntax = tree.GetRoot()
            .DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
        IPropertySymbol propSymbol = model.GetDeclaredSymbol(propSyntax)!;
        INamedTypeSymbol propType = (INamedTypeSymbol)propSymbol.Type;

        Assert.True(propType.IsGenericType);
        Assert.True(((ISymbol)propType).IsInstanceOf(propType.ConstructedFrom));
    }

    [Fact]
    public void IsInstanceOf_NonGenericNamedType_ReturnsTrue()
    {
        string code = "public class MyClass {}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        ClassDeclarationSyntax classSyntax = tree.GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        INamedTypeSymbol type = model.GetDeclaredSymbol(classSyntax)!;

        Assert.True(((ISymbol)type).IsInstanceOf(type));
    }

    [Fact]
    public void IsInstanceOf_NamedTypeNotMatching_ReturnsFalse()
    {
        string code = @"
public class A {}
public class B {}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        INamedTypeSymbol[] types = tree.GetRoot()
            .DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Select(c => model.GetDeclaredSymbol(c)!)
            .ToArray();

        Assert.False(((ISymbol)types[0]).IsInstanceOf(types[1]));
    }

    [Fact]
    public void IsInstanceOf_DefaultFallback_FieldSymbolMatching_ReturnsTrue()
    {
        string code = "public class C { public int F; }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        VariableDeclaratorSyntax fieldSyntax = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
        IFieldSymbol field = (IFieldSymbol)model.GetDeclaredSymbol(fieldSyntax)!;

        Assert.True(field.IsInstanceOf(field));
    }

    [Fact]
    public void IsInstanceOf_DefaultFallback_FieldSymbolNotMatching_ReturnsFalse()
    {
        string code = "public class C { public int F1; public int F2; }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IFieldSymbol[] fields = tree.GetRoot()
            .DescendantNodes().OfType<VariableDeclaratorSyntax>()
            .Select(v => (IFieldSymbol)model.GetDeclaredSymbol(v)!)
            .ToArray();

        Assert.False(fields[0].IsInstanceOf(fields[1]));
    }

    [Fact]
    public void IsInstanceOf_ImmutableArray_MatchFound_ReturnsTrueWithSymbol()
    {
        string code = @"
public class C
{
    public void M1() {}
    public void M2() {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol[] methods = GetMethodSymbols(model, tree)
            .Where(m => m.Name.StartsWith("M", StringComparison.Ordinal))
            .ToArray();
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        ImmutableArray<IMethodSymbol> candidates = methods.ToImmutableArray();

        bool result = ((ISymbol)methods[0]).IsInstanceOf(candidates, out IMethodSymbol? match);
#pragma warning restore ECS0900 // Minimize boxing and unboxing

        Assert.True(result);
        Assert.NotNull(match);
        Assert.Equal(methods[0].Name, match.Name);
    }

    [Fact]
    public void IsInstanceOf_ImmutableArray_NoMatch_ReturnsFalseWithNull()
    {
        string code = @"
public class C
{
    public void M1() {}
    public void M2() {}
    public void M3() {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol[] methods = GetMethodSymbols(model, tree)
            .Where(m => m.Name.StartsWith("M", StringComparison.Ordinal))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToArray();
        ImmutableArray<IMethodSymbol> candidates = ImmutableArray.Create(methods[1], methods[2]);

        bool result = ((ISymbol)methods[0]).IsInstanceOf(candidates, out IMethodSymbol? match);

        Assert.False(result);
        Assert.Null(match);
    }

    [Fact]
    public void IsInstanceOf_ImmutableArray_EmptyArray_ReturnsFalse()
    {
        string code = "public class C { public void M() {} }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        ImmutableArray<IMethodSymbol> empty = ImmutableArray<IMethodSymbol>.Empty;

        bool result = ((ISymbol)method).IsInstanceOf(empty, out IMethodSymbol? match);

        Assert.False(result);
        Assert.Null(match);
    }

    [Fact]
    public void IsInstanceOf_ImmutableArraySimple_MatchFound_ReturnsTrue()
    {
        string code = "public class C { public void M() {} }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        ImmutableArray<IMethodSymbol> candidates = ImmutableArray.Create(method);

        Assert.True(((ISymbol)method).IsInstanceOf(candidates));
    }

    [Fact]
    public void IsInstanceOf_ImmutableArraySimple_NoMatch_ReturnsFalse()
    {
        string code = @"
public class C
{
    public void M1() {}
    public void M2() {}
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol[] methods = GetMethodSymbols(model, tree)
            .Where(m => m.Name.StartsWith("M", StringComparison.Ordinal))
            .ToArray();
        ImmutableArray<IMethodSymbol> candidates = ImmutableArray.Create(methods[1]);

        Assert.False(((ISymbol)methods[0]).IsInstanceOf(candidates));
    }

    [Fact]
    public void IsOverridable_StaticMethod_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(
            "public class C { public static void M() {} }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        Assert.False(((ISymbol)method).IsOverridable());
    }

    [Fact]
    public void IsOverridable_StaticProperty_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(
            "public class C { public static int P { get; set; } }");
        IPropertySymbol prop = tree.GetRoot()
            .DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => model.GetDeclaredSymbol(p))
            .OfType<IPropertySymbol>()
            .First();
        Assert.False(((ISymbol)prop).IsOverridable());
    }

    [Fact]
    public void IsOverridable_InterfaceMember_ReturnsTrue()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(
            "public interface I { void M(); }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        Assert.True(((ISymbol)method).IsOverridable());
    }

    [Fact]
    public void IsOverridable_VirtualMethod_ReturnsTrue()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(
            "public class C { public virtual void M() {} }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        Assert.True(((ISymbol)method).IsOverridable());
    }

    [Fact]
    public void IsOverridable_AbstractMethod_ReturnsTrue()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(
            "public abstract class C { public abstract void M(); }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        Assert.True(((ISymbol)method).IsOverridable());
    }

    [Fact]
    public void IsOverridable_OverrideMethod_ReturnsTrue()
    {
        string code = @"
public class Base { public virtual void M() {} }
public class Derived : Base { public override void M() {} }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal) && m.IsOverride);
        Assert.True(((ISymbol)method).IsOverridable());
    }

    [Fact]
    public void IsOverridable_SealedOverrideMethod_ReturnsFalse()
    {
        string code = @"
public class Base { public virtual void M() {} }
public class Derived : Base { public sealed override void M() {} }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal) && m.IsSealed);
        Assert.False(((ISymbol)method).IsOverridable());
    }

    [Fact]
    public void IsOverridable_RegularNonVirtualMethod_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(
            "public class C { public void M() {} }");
        IMethodSymbol method = GetMethodSymbols(model, tree)
            .First(m => string.Equals(m.Name, "M", StringComparison.Ordinal));
        Assert.False(((ISymbol)method).IsOverridable());
    }

    [Fact]
    public void IsOverridable_InterfaceProperty_ReturnsTrue()
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(
            "public interface I { int P { get; } }");
        IPropertySymbol prop = tree.GetRoot()
            .DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => model.GetDeclaredSymbol(p))
            .OfType<IPropertySymbol>()
            .First();
        Assert.True(((ISymbol)prop).IsOverridable());
    }

    [Fact]
    public void IsTaskOrValueResultProperty_TaskGenericResultProperty_ReturnsTrue()
    {
        string code = @"
using System.Threading.Tasks;
public class C
{
    public void M()
    {
        Task<int> t = Task.FromResult(42);
        int r = t.Result;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Result");
        Assert.True(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsTaskOrValueResultProperty_ValueTaskGenericResultProperty_ReturnsTrue()
    {
        string code = @"
using System.Threading.Tasks;
public class C
{
    public ValueTask<int> GetValue() => new ValueTask<int>(42);
    public void M()
    {
        var vt = GetValue();
        int r = vt.Result;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Result");
        Assert.True(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsTaskOrValueResultProperty_NonResultProperty_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class C
{
    public void M()
    {
        Task<int> t = Task.FromResult(42);
        bool b = t.IsCompleted;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "IsCompleted");
        Assert.False(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsTaskOrValueResultProperty_NonPropertySymbol_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class C
{
    public void M()
    {
        Task<int> t = Task.FromResult(42);
    }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        InvocationExpressionSyntax invocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        ISymbol? methodSymbol = model.GetSymbolInfo(invocation).Symbol;
        Assert.NotNull(methodSymbol);
        Assert.False(methodSymbol.IsTaskOrValueResultProperty(knownSymbols));
    }

#pragma warning disable ECS0900 // Minimize boxing and unboxing
    [Theory]
    [InlineData("System.Threading.Tasks.Task", true)]
    [InlineData("System.Threading.Tasks.Task<int>", true)]
    [InlineData("System.Threading.Tasks.ValueTask", true)]
    [InlineData("System.Threading.Tasks.ValueTask<int>", true)]
    [InlineData("string", false)]
    public void IsTaskOrValueTaskType_VariousTypes_ReturnsExpected(string typeName, bool expected)
    {
        string code = $@"
using System.Threading.Tasks;
public class C
{{
    public {typeName} GetValue() => default!;
}}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        MethodDeclarationSyntax methodSyntax = tree.GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        IMethodSymbol method = model.GetDeclaredSymbol(methodSyntax)!;
        ITypeSymbol returnType = method.ReturnType;

        Assert.Equal(expected, returnType.IsTaskOrValueTaskType(knownSymbols));
    }
#pragma warning restore ECS0900 // Minimize boxing and unboxing

    [Fact]
    public void IsTaskOrValueTaskType_NonNamedTypeSymbol_ReturnsFalse()
    {
        string code = @"
public class C
{
    public int[] GetValues() => new[] { 1 };
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        MethodDeclarationSyntax methodSyntax = tree.GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        IMethodSymbol method = model.GetDeclaredSymbol(methodSyntax)!;

        // int[] is an IArrayTypeSymbol, not INamedTypeSymbol
        Assert.IsNotAssignableFrom<INamedTypeSymbol>(method.ReturnType);
        Assert.False(method.ReturnType.IsTaskOrValueTaskType(knownSymbols));
    }

    [Fact]
    public async Task IsMoqSetupMethod_SetupCall_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValue());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Setup");
        Assert.True(symbol.IsMoqSetupMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqSetupMethod_NonSetupCall_ReturnsFalse()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Verify(x => x.GetValue());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Verify");
        Assert.False(symbol.IsMoqSetupMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqVerificationMethod_VerifyCall_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Verify(x => x.GetValue());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Verify");
        Assert.True(symbol.IsMoqVerificationMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqSetupSequenceMethod_SetupSequenceCall_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.SetupSequence(x => x.GetValue());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "SetupSequence");
        Assert.True(symbol.IsMoqSetupSequenceMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqReturnsMethod_ReturnsCall_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValue()).Returns(42);
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Returns");
        Assert.True(symbol.IsMoqReturnsMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqCallbackMethod_CallbackCall_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValue()).Callback(() => { });
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Callback");
        Assert.True(symbol.IsMoqCallbackMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqThrowsMethod_ThrowsCall_ReturnsTrue()
    {
        string code = @"
using Moq;
using System;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValue()).Throws(new Exception());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Throws");
        Assert.True(symbol.IsMoqThrowsMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqReturnValueSpecificationMethod_Returns_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValue()).Returns(42);
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Returns");
        Assert.True(symbol.IsMoqReturnValueSpecificationMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqReturnsAsyncMethod_ReturnsAsyncCall_ReturnsTrue()
    {
        string code = @"
using Moq;
using System.Threading.Tasks;
public interface IService { Task<int> GetValueAsync(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValueAsync()).ReturnsAsync(42);
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "ReturnsAsync");
        Assert.True(symbol.IsMoqReturnsAsyncMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqThrowsAsyncMethod_ThrowsAsyncCall_ReturnsTrue()
    {
        string code = @"
using Moq;
using System;
using System.Threading.Tasks;
public interface IService { Task<int> GetValueAsync(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValueAsync()).ThrowsAsync(new Exception());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "ThrowsAsync");
        Assert.True(symbol.IsMoqThrowsAsyncMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqEventSetupMethod_SetupAddCall_ReturnsTrue()
    {
        string code = @"
using Moq;
using System;
public interface IService { event EventHandler MyEvent; }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.SetupAdd(x => x.MyEvent += null);
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "SetupAdd");
        Assert.True(symbol.IsMoqEventSetupMethod(knownSymbols));
        Assert.True(symbol.IsMoqSetupAddMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqEventSetupMethod_SetupRemoveCall_ReturnsTrue()
    {
        string code = @"
using Moq;
using System;
public interface IService { event EventHandler MyEvent; }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.SetupRemove(x => x.MyEvent -= null);
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "SetupRemove");
        Assert.True(symbol.IsMoqEventSetupMethod(knownSymbols));
        Assert.True(symbol.IsMoqSetupRemoveMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqRaisesMethod_NonMethodSymbol_ReturnsFalse()
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(@"
using Moq;
public interface IService { int Value { get; } }
public class C { }");
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync();
        InterfaceDeclarationSyntax ifaceSyntax = root
            .DescendantNodes().OfType<InterfaceDeclarationSyntax>().First();
        INamedTypeSymbol ifaceSymbol = model.GetDeclaredSymbol(ifaceSyntax)!;

        Assert.False(((ISymbol)ifaceSymbol).IsMoqRaisesMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqVerificationMethod_VerifyGet_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int Value { get; } }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.VerifyGet(x => x.Value);
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "VerifyGet");
        Assert.True(symbol.IsMoqVerificationMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqVerificationMethod_VerifyNoOtherCalls_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.VerifyNoOtherCalls();
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "VerifyNoOtherCalls");
        Assert.True(symbol.IsMoqVerificationMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqReturnValueSpecificationMethod_Throws_ReturnsTrue()
    {
        string code = @"
using Moq;
using System;
public interface IService { int GetValue(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValue()).Throws(new Exception());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "Throws");
        Assert.True(symbol.IsMoqReturnValueSpecificationMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqReturnValueSpecificationMethod_ReturnsAsync_ReturnsTrue()
    {
        string code = @"
using Moq;
using System.Threading.Tasks;
public interface IService { Task<int> GetValueAsync(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValueAsync()).ReturnsAsync(1);
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "ReturnsAsync");
        Assert.True(symbol.IsMoqReturnValueSpecificationMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqReturnValueSpecificationMethod_ThrowsAsync_ReturnsTrue()
    {
        string code = @"
using Moq;
using System;
using System.Threading.Tasks;
public interface IService { Task<int> GetValueAsync(); }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.GetValueAsync()).ThrowsAsync(new Exception());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "ThrowsAsync");
        Assert.True(symbol.IsMoqReturnValueSpecificationMethod(knownSymbols));
    }

    [Fact]
    public async Task IsMoqVerificationMethod_VerifySet_ReturnsTrue()
    {
        string code = @"
using Moq;
public interface IService { string Name { get; set; } }
public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.VerifySet(x => x.Name = It.IsAny<string>());
    }
}";
        (ISymbol symbol, MoqKnownSymbols knownSymbols) = await GetMoqInvocationSymbol(code, "VerifySet");
        Assert.True(symbol.IsMoqVerificationMethod(knownSymbols));
    }

    private static IMethodSymbol GetConstructor(string code, Accessibility expectedAccessibility)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        return GetMethodSymbols(model, tree)
            .First(m => m.MethodKind == MethodKind.Constructor
                        && m.DeclaredAccessibility == expectedAccessibility);
    }

    private static IEnumerable<IMethodSymbol> GetMethodSymbols(SemanticModel model, SyntaxTree tree)
    {
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Select(t => model.GetDeclaredSymbol(t))
            .OfType<INamedTypeSymbol>()
            .SelectMany(t => t.GetMembers().OfType<IMethodSymbol>());
    }

    private static (IPropertySymbol Property, MoqKnownSymbols KnownSymbols) GetPropertyFromMemberAccess(
        string code,
        string propertyName)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        MemberAccessExpressionSyntax memberAccess = tree.GetRoot()
            .DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .First(m => string.Equals(m.Name.Identifier.Text, propertyName, StringComparison.Ordinal));
        IPropertySymbol prop = (IPropertySymbol)model.GetSymbolInfo(memberAccess).Symbol!;
        return (prop, knownSymbols);
    }

    private static async Task<(ISymbol Symbol, MoqKnownSymbols KnownSymbols)> GetMoqInvocationSymbol(
        string code,
        string methodName)
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(code).ConfigureAwait(false);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        SyntaxNode root = await tree.GetRootAsync().ConfigureAwait(false);
        InvocationExpressionSyntax invocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression switch
            {
                MemberAccessExpressionSyntax ma =>
                    string.Equals(ma.Name.Identifier.Text, methodName, StringComparison.Ordinal),
                _ => false,
            });
        ISymbol symbol = model.GetSymbolInfo(invocation).Symbol!;
        return (symbol, knownSymbols);
    }
}
