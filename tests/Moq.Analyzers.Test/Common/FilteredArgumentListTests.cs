using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moq.Analyzers.Test.Common;

public class FilteredArgumentListTests
{
    [Fact]
    public void Count_NoSkip_ReturnsFullCount()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: -1);

        Assert.Equal(3, sut.Count);
    }

    [Fact]
    public void Count_WithSkip_ReturnsCountMinusOne()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: 1);

        Assert.Equal(2, sut.Count);
    }

    [Fact]
    public void Count_NullArgumentList_ReturnsZero()
    {
        FilteredArgumentList sut = new(null, skipIndex: -1);

        Assert.Equal(0, sut.Count);
    }

    [Fact]
    public void Count_NullArgumentListWithSkipIndex_ReturnsZero()
    {
        FilteredArgumentList sut = new(null, skipIndex: 0);

        Assert.Equal(0, sut.Count);
    }

    [Fact]
    public void Count_EmptyArgumentList_ReturnsZero()
    {
        ArgumentListSyntax argList = CreateArgumentList();
        FilteredArgumentList sut = new(argList, skipIndex: -1);

        Assert.Equal(0, sut.Count);
    }

    [Fact]
    public void Indexer_SkipIndexZero_RemapsCorrectly()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: 0);

        Assert.Equal("b", sut[0].Expression.ToString());
        Assert.Equal("c", sut[1].Expression.ToString());
    }

    [Fact]
    public void Indexer_SkipIndexMiddle_RemapsCorrectly()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: 1);

        Assert.Equal("a", sut[0].Expression.ToString());
        Assert.Equal("c", sut[1].Expression.ToString());
    }

    [Fact]
    public void Indexer_SkipIndexLast_RemapsCorrectly()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: 2);

        Assert.Equal("a", sut[0].Expression.ToString());
        Assert.Equal("b", sut[1].Expression.ToString());
    }

    [Fact]
    public void Indexer_NoSkip_ReturnsOriginalOrder()
    {
        ArgumentListSyntax argList = CreateArgumentList("x", "y", "z");
        FilteredArgumentList sut = new(argList, skipIndex: -1);

        Assert.Equal("x", sut[0].Expression.ToString());
        Assert.Equal("y", sut[1].Expression.ToString());
        Assert.Equal("z", sut[2].Expression.ToString());
    }

    [Fact]
    public void SkipIndex_Negative_FallsBackToNoSkip()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b");
        FilteredArgumentList sut = new(argList, skipIndex: -5);

        Assert.Equal(2, sut.Count);
        Assert.Equal("a", sut[0].Expression.ToString());
        Assert.Equal("b", sut[1].Expression.ToString());
    }

    [Fact]
    public void SkipIndex_BeyondCount_FallsBackToNoSkip()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b");
        FilteredArgumentList sut = new(argList, skipIndex: 10);

        Assert.Equal(2, sut.Count);
        Assert.Equal("a", sut[0].Expression.ToString());
        Assert.Equal("b", sut[1].Expression.ToString());
    }

    [Fact]
    public void FormatArguments_EmptyList_ReturnsEmptyParens()
    {
        ArgumentListSyntax argList = CreateArgumentList();
        FilteredArgumentList sut = new(argList, skipIndex: -1);

        Assert.Equal("()", sut.FormatArguments());
    }

    [Fact]
    public void FormatArguments_NullList_ReturnsEmptyParens()
    {
        FilteredArgumentList sut = new(null, skipIndex: -1);

        Assert.Equal("()", sut.FormatArguments());
    }

    [Fact]
    public void FormatArguments_SingleArgument_NoSkip()
    {
        ArgumentListSyntax argList = CreateArgumentList("arg1");
        FilteredArgumentList sut = new(argList, skipIndex: -1);

        Assert.Equal("(arg1)", sut.FormatArguments());
    }

    [Fact]
    public void FormatArguments_SingleArgument_Skipped_ReturnsEmptyParens()
    {
        ArgumentListSyntax argList = CreateArgumentList("arg1");
        FilteredArgumentList sut = new(argList, skipIndex: 0);

        Assert.Equal("()", sut.FormatArguments());
    }

    [Fact]
    public void FormatArguments_MultipleArguments_NoSkip()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: -1);

        Assert.Equal("(a, b, c)", sut.FormatArguments());
    }

    [Fact]
    public void FormatArguments_MultipleArguments_SkipFirst()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: 0);

        Assert.Equal("(b, c)", sut.FormatArguments());
    }

    [Fact]
    public void FormatArguments_MultipleArguments_SkipMiddle()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: 1);

        Assert.Equal("(a, c)", sut.FormatArguments());
    }

    [Fact]
    public void FormatArguments_MultipleArguments_SkipLast()
    {
        ArgumentListSyntax argList = CreateArgumentList("a", "b", "c");
        FilteredArgumentList sut = new(argList, skipIndex: 2);

        Assert.Equal("(a, b)", sut.FormatArguments());
    }

    private static ArgumentListSyntax CreateArgumentList(params string[] expressions)
    {
        SeparatedSyntaxList<ArgumentSyntax> args = SyntaxFactory.SeparatedList(
            expressions.Select(e =>
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression(e))));

        return SyntaxFactory.ArgumentList(args);
    }
}
