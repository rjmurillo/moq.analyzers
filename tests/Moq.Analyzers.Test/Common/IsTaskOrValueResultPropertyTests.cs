using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public class IsTaskOrValueResultPropertyTests
{
#pragma warning disable ECS1300 // Static field init is simpler than static constructor for single field
    private static readonly MetadataReference SystemThreadingTasksReference =
        MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location);
#pragma warning restore ECS1300

    private static MetadataReference[] CoreReferencesWithTasks =>
        [CompilationHelper.CorlibReference, CompilationHelper.SystemRuntimeReference, SystemThreadingTasksReference, CompilationHelper.SystemLinqReference];

    // Positive cases: Result property on Task<T> and ValueTask<T> should return true.
    [Theory]
    [InlineData("Task<int> t = Task.FromResult(42); int r = t.Result;", "Result")]
    [InlineData("Task<string> t = Task.FromResult(\"hello\"); string r = t.Result;", "Result")]
    public void IsTaskOrValueResultProperty_TaskGenericResult_ReturnsTrue(string statement, string propertyName)
    {
        string code = $@"
using System.Threading.Tasks;
public class C
{{
    public void M()
    {{
        {statement}
    }}
}}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, propertyName);
        Assert.True(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Theory]
    [InlineData("int", "42")]
    [InlineData("string", "\"hello\"")]
    public void IsTaskOrValueResultProperty_ValueTaskGenericResult_ReturnsTrue(string typeArg, string value)
    {
        string code = $@"
using System.Threading.Tasks;
public class C
{{
    public ValueTask<{typeArg}> GetValue() => new ValueTask<{typeArg}>({value});
    public void M()
    {{
        var vt = GetValue();
        {typeArg} r = vt.Result;
    }}
}}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Result");
        Assert.True(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    // Negative cases: non-Result properties, non-Task types, and non-property symbols.
    [Fact]
    public void IsTaskOrValueResultProperty_MethodSymbol_ReturnsFalse()
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
        Assert.IsAssignableFrom<IMethodSymbol>(methodSymbol);
        Assert.False(methodSymbol.IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsTaskOrValueResultProperty_FieldSymbol_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class C
{
    public int Result;
    public void M()
    {
        int r = this.Result;
    }
}";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code, CoreReferencesWithTasks);
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);
        MemberAccessExpressionSyntax memberAccess = tree.GetRoot()
            .DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .First(m => string.Equals(m.Name.Identifier.Text, "Result", StringComparison.Ordinal));
        ISymbol? symbol = model.GetSymbolInfo(memberAccess).Symbol;

        Assert.NotNull(symbol);
        Assert.IsAssignableFrom<IFieldSymbol>(symbol);
        Assert.False(symbol.IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsTaskOrValueResultProperty_ResultPropertyOnCustomType_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class MyClass
{
    public int Result { get; set; }
}
public class C
{
    public void M()
    {
        var obj = new MyClass();
        int r = obj.Result;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Result");
        Assert.False(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsTaskOrValueResultProperty_NonResultPropertyOnTask_ReturnsFalse()
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
    public void IsTaskOrValueResultProperty_NonGenericTaskProperty_ReturnsFalse()
    {
        // Non-generic Task has no Result property. Use IsCompleted as a stand-in
        // to verify that properties on non-generic Task are not flagged.
        string code = @"
using System.Threading.Tasks;
public class C
{
    public void M()
    {
        Task t = Task.CompletedTask;
        bool b = t.IsCompleted;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "IsCompleted");
        Assert.False(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsTaskOrValueResultProperty_CustomClassWithResultProperty_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class OperationResult<T>
{
    public T Result { get; set; }
}
public class C
{
    public void M()
    {
        var op = new OperationResult<int>();
        int r = op.Result;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Result");
        Assert.False(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    // IsGenericResultProperty negative cases: non-generic types, wrong property names, unrelated generic types.
    [Fact]
    public void IsGenericResultProperty_NonGenericContainingType_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class NonGenericHolder
{
    public int Result { get; set; }
}
public class C
{
    public void M()
    {
        var holder = new NonGenericHolder();
        int r = holder.Result;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Result");
        Assert.False(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsGenericResultProperty_PropertyNotNamedResult_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class C
{
    public void M()
    {
        Task<int> t = Task.FromResult(42);
        var s = t.Status;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Status");
        Assert.False(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
    }

    [Fact]
    public void IsGenericResultProperty_ResultOnUnrelatedGenericType_ReturnsFalse()
    {
        string code = @"
using System.Threading.Tasks;
public class MyWrapper<T>
{
    public T Result { get; set; }
}
public class C
{
    public void M()
    {
        var wrapper = new MyWrapper<int>();
        int r = wrapper.Result;
    }
}";
        (IPropertySymbol prop, MoqKnownSymbols knownSymbols) = GetPropertyFromMemberAccess(code, "Result");
        Assert.False(((ISymbol)prop).IsTaskOrValueResultProperty(knownSymbols));
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
}
