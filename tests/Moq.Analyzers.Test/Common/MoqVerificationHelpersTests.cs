using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers.Test.Common;

public class MoqVerificationHelpersTests
{
    [Fact]
    public void ExtractLambdaFromArgument_ReturnsLambda_ForDirectLambda()
    {
        string code = @"class C { void M() { System.Action a = () => { }; } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);
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
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);
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
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);
        IdentifierNameSyntax methodGroup = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => string.Equals(id.Identifier.Text, "M2", StringComparison.Ordinal));
        IOperation? operation = model.GetOperation(methodGroup);
        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(operation!);
        Assert.Null(result);
    }

    [Fact]
    public void ExtractLambdaFromArgument_ReturnsNull_ForNullInput()
    {
        // Suppress CS8625: Cannot convert null literal to non-nullable reference type
#pragma warning disable CS8625
        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(null);
#pragma warning restore CS8625
        Assert.Null(result);
    }

    [Fact]
    public void ExtractLambdaFromArgument_ReturnsLambda_ForNestedLambda()
    {
        string code = @"class C { void M() { System.Func<int, System.Action> f = x => () => { }; } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);

        // Find the inner (nested) lambda
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
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        SemanticModel model = compilation.GetSemanticModel(tree);
        LiteralExpressionSyntax literal = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().First();
        IOperation? operation = model.GetOperation(literal);
        IAnonymousFunctionOperation? result = MoqVerificationHelpers.ExtractLambdaFromArgument(operation!);
        Assert.Null(result);
    }
}
