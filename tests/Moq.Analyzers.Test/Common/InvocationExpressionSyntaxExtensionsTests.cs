using Moq.Analyzers.Common.WellKnown;

namespace Moq.Analyzers.Test.Common;

public class InvocationExpressionSyntaxExtensionsTests
{
    [Fact]
    public void FindMockedMethodInvocationFromSetupMethod_ValidLambdaWithMethodInvocation_ReturnsInvocation()
    {
        const string code = @"mock.Setup(x => x.Method())";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        InvocationExpressionSyntax setupInvocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression.ToString().Contains("Setup"));

        InvocationExpressionSyntax? result = setupInvocation.FindMockedMethodInvocationFromSetupMethod();

        Assert.NotNull(result);
        Assert.Equal("x.Method()", result.ToString());
    }

    [Fact]
    public void FindMockedMethodInvocationFromSetupMethod_NullInput_ReturnsNull()
    {
        InvocationExpressionSyntax? nullInvocation = null;

        InvocationExpressionSyntax? result = nullInvocation.FindMockedMethodInvocationFromSetupMethod();

        Assert.Null(result);
    }

    [Fact]
    public void FindMockedMethodInvocationFromSetupMethod_NonLambdaArgument_ReturnsNull()
    {
        // Setup with a non-lambda argument (string literal)
        const string code = @"mock.Setup(""notALambda"")";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        InvocationExpressionSyntax setupInvocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        InvocationExpressionSyntax? result = setupInvocation.FindMockedMethodInvocationFromSetupMethod();

        Assert.Null(result);
    }

    [Fact]
    public void FindMockedMethodInvocationFromSetupMethod_LambdaBodyIsPropertyAccess_ReturnsNull()
    {
        // Lambda body is a MemberAccessExpressionSyntax, not an InvocationExpressionSyntax
        const string code = @"mock.Setup(x => x.Property)";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        InvocationExpressionSyntax setupInvocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        InvocationExpressionSyntax? result = setupInvocation.FindMockedMethodInvocationFromSetupMethod();

        Assert.Null(result);
    }

    [Fact]
    public void FindMockedMemberExpressionFromSetupMethod_ValidLambda_ReturnsMemberExpression()
    {
        const string code = @"mock.Setup(x => x.Property)";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        InvocationExpressionSyntax setupInvocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        ExpressionSyntax? result = setupInvocation.FindMockedMemberExpressionFromSetupMethod();

        Assert.NotNull(result);
        Assert.Equal("x.Property", result.ToString());
    }

    [Fact]
    public void FindMockedMemberExpressionFromSetupMethod_NullInput_ReturnsNull()
    {
        InvocationExpressionSyntax? nullInvocation = null;

        ExpressionSyntax? result = nullInvocation.FindMockedMemberExpressionFromSetupMethod();

        Assert.Null(result);
    }

    [Fact]
    public void FindMockedMemberExpressionFromSetupMethod_NonLambdaArgument_ReturnsNull()
    {
        const string code = @"mock.Setup(""notALambda"")";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        InvocationExpressionSyntax setupInvocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        ExpressionSyntax? result = setupInvocation.FindMockedMemberExpressionFromSetupMethod();

        Assert.Null(result);
    }

    [Fact]
    public async Task IsRaisesMethodCall_ValidRaisesCall_ReturnsTrue()
    {
        const string code = @"
using System;
using Moq;

public interface IService
{
    event EventHandler MyEvent;
    void DoWork();
}

public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.DoWork()).Raises(x => x.MyEvent += null, EventArgs.Empty);
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(code);
        SyntaxNode root = await tree.GetRootAsync();
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);

        // Find the Raises invocation
        InvocationExpressionSyntax raisesInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                        && string.Equals(ma.Name.Identifier.Text, "Raises", StringComparison.Ordinal));

        bool result = raisesInvocation.IsRaisesMethodCall(model, knownSymbols);

        Assert.True(result);
    }

    [Fact]
    public async Task IsRaisesMethodCall_NonRaisesMethod_ReturnsFalse()
    {
        const string code = @"
using System;
using Moq;

public interface IService
{
    void DoWork();
}

public class C
{
    public void M()
    {
        var mock = new Mock<IService>();
        mock.Setup(x => x.DoWork());
    }
}";
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(code);
        SyntaxNode root = await tree.GetRootAsync();
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);

        // Find the Setup invocation (not Raises)
        InvocationExpressionSyntax setupInvocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is MemberAccessExpressionSyntax ma
                        && string.Equals(ma.Name.Identifier.Text, "Setup", StringComparison.Ordinal));

        bool result = setupInvocation.IsRaisesMethodCall(model, knownSymbols);

        Assert.False(result);
    }

    [Fact]
    public async Task IsRaisesMethodCall_ExpressionNotMemberAccess_ReturnsFalse()
    {
        // A direct method call (not member access) such as a local function call
        const string code = @"
using System;
using Moq;

public class C
{
    public void M()
    {
        DoSomething();
    }

    private static void DoSomething() { }
}";
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(code);
        SyntaxNode root = await tree.GetRootAsync();
        MoqKnownSymbols knownSymbols = new MoqKnownSymbols(model.Compilation);

        InvocationExpressionSyntax invocation = root
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        bool result = invocation.IsRaisesMethodCall(model, knownSymbols);

        Assert.False(result);
    }
}
