using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Moq.Analyzers;
using Xunit;

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
