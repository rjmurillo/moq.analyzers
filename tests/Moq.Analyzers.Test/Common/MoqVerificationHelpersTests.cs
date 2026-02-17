using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers.Test.Common;

public class MoqVerificationHelpersTests
{
    [Fact]
    public void ExtractLambdaFromArgument_ReturnsLambda_ForDirectLambda()
    {
        string code = @"class C { void M() { System.Action a = () => { }; } }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        ParenthesizedLambdaExpressionSyntax lambda = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().First();
        IOperation? operation = model.GetOperation(lambda);

        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(operation!);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IAnonymousFunctionOperation>(result);
    }

    [Fact]
    public void ExtractLambdaFromArgument_ReturnsLambda_ForSimpleLambda()
    {
        string code = @"class C { void M() { System.Func<int, string> f = x => x.ToString(); } }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        SimpleLambdaExpressionSyntax lambda = tree.GetRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().First();
        IOperation? operation = model.GetOperation(lambda);

        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(operation!);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IAnonymousFunctionOperation>(result);
    }

    [Fact]
    public void ExtractLambdaFromArgument_ReturnsNull_ForMethodGroupConversion()
    {
        string code = @"class C { void M() { System.Action a = M2; } void M2() { } }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        IdentifierNameSyntax methodGroup = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
            .First(id => string.Equals(id.Identifier.Text, "M2", StringComparison.Ordinal));
        IOperation? operation = model.GetOperation(methodGroup);

        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(operation!);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractLambdaFromArgument_ReturnsLambda_ForNestedLambda()
    {
        string code = @"class C { void M() { System.Func<int, System.Action> f = x => () => { }; } }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        ParenthesizedLambdaExpressionSyntax innerLambda = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().First();
        IOperation? operation = model.GetOperation(innerLambda);

        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(operation!);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IAnonymousFunctionOperation>(result);
    }

    [Fact]
    public void ExtractLambdaFromArgument_ReturnsNull_ForNonLambda()
    {
        string code = @"class C { void M() { int x = 1; } }";
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        LiteralExpressionSyntax literal = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        IOperation? operation = model.GetOperation(literal);

        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(operation!);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractPropertyFromVerifySetLambda_AssignmentWithPropertyRef_ReturnsProperty()
    {
        const string code = @"
using Moq;

public interface IFoo
{
    int Value { get; set; }
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.VerifySet(x => x.Value = 42);
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "VerifySet");
        IAnonymousFunctionOperation? lambda = MoqVerificationHelpers.ExtractLambdaFromArgument(invocation.Arguments[0].Value);
        Assert.NotNull(lambda);

        ISymbol? result = MoqVerificationHelpers.ExtractPropertyFromVerifySetLambda(lambda!);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IPropertySymbol>(result);
        Assert.Equal("Value", result!.Name);
    }

    [Fact]
    public async Task ExtractPropertyFromVerifySetLambda_NonAssignmentExpression_ReturnsNull()
    {
        // Use a lambda whose body contains an ExpressionStatementOperation that is NOT an assignment.
        // Calling a method on the mock parameter produces an IExpressionStatementOperation wrapping
        // an IInvocationOperation (not an IAssignmentOperation).
        const string code = @"
using Moq;

public interface IFoo
{
    void DoSomething();
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.VerifySet(x => { x.DoSomething(); });
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "VerifySet");
        IAnonymousFunctionOperation? lambda = MoqVerificationHelpers.ExtractLambdaFromArgument(invocation.Arguments[0].Value);
        Assert.NotNull(lambda);

        ISymbol? result = MoqVerificationHelpers.ExtractPropertyFromVerifySetLambda(lambda!);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExtractPropertyFromVerifySetLambda_EmptyBody_ReturnsNull()
    {
        // A lambda with an empty block body { } still has a body with zero user operations
        // (only an implicit return). The method iterates Body.Operations and finds nothing.
        const string code = @"
using Moq;

public interface IFoo
{
    int Value { get; set; }
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.VerifySet(x => { });
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "VerifySet");
        IAnonymousFunctionOperation? lambda = MoqVerificationHelpers.ExtractLambdaFromArgument(invocation.Arguments[0].Value);
        Assert.NotNull(lambda);

        ISymbol? result = MoqVerificationHelpers.ExtractPropertyFromVerifySetLambda(lambda!);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetMockedMemberSymbol_ZeroArguments_ReturnsNull()
    {
        // Verify() with no arguments produces an IInvocationOperation with 0 args.
        const string code = @"
using Moq;

public interface IFoo
{
    void DoSomething();
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Verify();
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "Verify");

        ISymbol? result = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocation);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetMockedMemberSymbol_LambdaWithMethodInvocation_ReturnsMethodSymbol()
    {
        const string code = @"
using Moq;

public interface IFoo
{
    int DoSomething();
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.DoSomething());
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "Setup");

        ISymbol? result = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocation);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IMethodSymbol>(result);
        Assert.Equal("DoSomething", result!.Name);
    }

    [Fact]
    public async Task TryGetMockedMemberSymbol_LambdaWithPropertyAccess_ReturnsPropertySymbol()
    {
        const string code = @"
using Moq;

public interface IFoo
{
    int Value { get; }
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Value);
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "Setup");

        ISymbol? result = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocation);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IPropertySymbol>(result);
        Assert.Equal("Value", result!.Name);
    }

    [Fact]
    public async Task TryGetMockedMemberSyntax_ZeroArguments_ReturnsNull()
    {
        const string code = @"
using Moq;

public interface IFoo
{
    void DoSomething();
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Verify();
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "Verify");

        SyntaxNode? result = MoqVerificationHelpers.TryGetMockedMemberSyntax(invocation);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetMockedMemberSyntax_LambdaWithMethodInvocation_ReturnsSyntaxNode()
    {
        const string code = @"
using Moq;

public interface IFoo
{
    int DoSomething();
}

public class Test
{
    public void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.DoSomething());
    }
}";
        IInvocationOperation invocation = await GetMoqInvocationAsync(code, "Setup");

        SyntaxNode? result = MoqVerificationHelpers.TryGetMockedMemberSyntax(invocation);

        Assert.NotNull(result);
        Assert.Contains("DoSomething", result!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetMockedMemberSyntax_NonLambdaArgument_ReturnsNull()
    {
        // When the first argument is not a lambda, ExtractLambdaFromArgument returns null,
        // causing TryGetMockedMemberSyntax to return null via null propagation.
        const string code = @"
class C
{
    static void Call(object arg) { }
    void M()
    {
        Call(42);
    }
}";
        IInvocationOperation invocation = GetInvocationOperation(code, "Call");

        SyntaxNode? result = MoqVerificationHelpers.TryGetMockedMemberSyntax(invocation);

        Assert.Null(result);
    }

    [Fact]
    public void TryGetMockedMemberSymbol_NonLambdaArgument_ReturnsNull()
    {
        // When the first argument is not a lambda, ExtractLambdaFromArgument returns null.
        const string code = @"
class C
{
    static void Call(object arg) { }
    void M()
    {
        Call(42);
    }
}";
        IInvocationOperation invocation = GetInvocationOperation(code, "Call");

        ISymbol? result = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocation);

        Assert.Null(result);
    }

    private static IInvocationOperation GetInvocationOperation(string code, string methodName)
    {
        (SemanticModel model, SyntaxTree tree) = CompilationHelper.CreateCompilation(code);
        InvocationExpressionSyntax invocationSyntax = tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(inv => inv.Expression.ToString().Contains(methodName, StringComparison.Ordinal));
        IOperation? operation = model.GetOperation(invocationSyntax);
        Assert.NotNull(operation);
        Assert.IsAssignableFrom<IInvocationOperation>(operation);
        return (IInvocationOperation)operation!;
    }

    private static async Task<IInvocationOperation> GetMoqInvocationAsync(string code, string methodName)
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(code).ConfigureAwait(false);
        SyntaxNode root = await tree.GetRootAsync().ConfigureAwait(false);
        InvocationExpressionSyntax invocationSyntax = root
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(inv =>
            {
                string text = inv.Expression.ToString();
                return text.Contains(methodName, StringComparison.Ordinal);
            });
        IOperation? operation = model.GetOperation(invocationSyntax);
        Assert.NotNull(operation);
        Assert.IsAssignableFrom<IInvocationOperation>(operation);
        return (IInvocationOperation)operation!;
    }
}
