namespace Moq.Analyzers.Test.Common;

public class InvocationExpressionSyntaxExtensionsTests
{
    [Fact]
    public void FindMockedMethodInvocationFromSetupMethod_WithValidSetup_ReturnsInvocation()
    {
        // Arrange
        const string code = @"
using Moq;

interface IFoo
{
    int GetValue();
}

class C
{
    void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.GetValue()).Returns(42);
    }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

        // Find the Setup invocation
        InvocationExpressionSyntax? setupInvocation = tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(i => i.Expression is MemberAccessExpressionSyntax mae && string.Equals(mae.Name.Identifier.Text, "Setup", StringComparison.Ordinal));

        Assert.NotNull(setupInvocation);

        // Act
        InvocationExpressionSyntax? result = setupInvocation!.FindMockedMethodInvocationFromSetupMethod();

        // Assert
        Assert.NotNull(result);
        MemberAccessExpressionSyntax? memberAccess = result!.Expression as MemberAccessExpressionSyntax;
        Assert.NotNull(memberAccess);
        Assert.Equal("GetValue", memberAccess!.Name.Identifier.Text);
    }

    [Fact]
    public void FindMockedMethodInvocationFromSetupMethod_WithNullInput_ReturnsNull()
    {
        // Act
        InvocationExpressionSyntax? result = ((InvocationExpressionSyntax?)null).FindMockedMethodInvocationFromSetupMethod();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindMockedMemberExpressionFromSetupMethod_WithPropertyAccess_ReturnsExpression()
    {
        // Arrange
        const string code = @"
using Moq;

interface IFoo
{
    int Value { get; set; }
}

class C
{
    void M()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Value).Returns(42);
    }
}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

        // Find the Setup invocation
        InvocationExpressionSyntax? setupInvocation = tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(i => i.Expression is MemberAccessExpressionSyntax mae && string.Equals(mae.Name.Identifier.Text, "Setup", StringComparison.Ordinal));

        Assert.NotNull(setupInvocation);

        // Act
        ExpressionSyntax? result = setupInvocation!.FindMockedMemberExpressionFromSetupMethod();

        // Assert
        Assert.NotNull(result);
        MemberAccessExpressionSyntax? memberAccess = result as MemberAccessExpressionSyntax;
        Assert.NotNull(memberAccess);
        Assert.Equal("Value", memberAccess!.Name.Identifier.Text);
    }

    [Fact]
    public void FindMockedMemberExpressionFromSetupMethod_WithNullInput_ReturnsNull()
    {
        // Act
        ExpressionSyntax? result = ((InvocationExpressionSyntax?)null).FindMockedMemberExpressionFromSetupMethod();

        // Assert
        Assert.Null(result);
    }
}
