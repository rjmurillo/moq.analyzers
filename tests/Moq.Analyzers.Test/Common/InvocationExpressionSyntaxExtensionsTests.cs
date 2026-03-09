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
    public void FindMockedMethodInvocationFromSetupMethod_EmptyArgumentList_ReturnsNull()
    {
        // An invocation with no arguments hits the Arguments.Count == 0 guard.
        const string code = @"mock.Setup()";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        InvocationExpressionSyntax setupInvocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        InvocationExpressionSyntax? result = setupInvocation.FindMockedMethodInvocationFromSetupMethod();

        Assert.Null(result);
    }

    [Fact]
    public void FindMockedMemberExpressionFromSetupMethod_EmptyArgumentList_ReturnsNull()
    {
        // An invocation with no arguments hits the Arguments.Count == 0 guard.
        const string code = @"mock.Setup()";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        InvocationExpressionSyntax setupInvocation = tree.GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        ExpressionSyntax? result = setupInvocation.FindMockedMemberExpressionFromSetupMethod();

        Assert.Null(result);
    }
}
