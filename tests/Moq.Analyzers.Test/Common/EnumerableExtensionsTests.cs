﻿namespace Moq.Analyzers.Test.Common;

public class EnumerableExtensionsTests
{
    [Fact]
    public void DefaultIfNotSingle_ReturnsNull_WhenSourceIsEmpty()
    {
        IEnumerable<int> source = [];
        int result = source.DefaultIfNotSingle();
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ReturnsElement_WhenSourceContainsSingleElement()
    {
        int[] source = [42];
        int result = source.DefaultIfNotSingle();
        Assert.Equal(42, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ReturnsNull_WhenSourceContainsMultipleElements()
    {
        int[] source = [1, 2, 3];
        int result = source.DefaultIfNotSingle();
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_WithPredicate_ReturnsNull_WhenNoElementsMatch()
    {
        int[] source = [1, 2, 3];
        int result = source.DefaultIfNotSingle(x => x > 10);
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_WithPredicate_ReturnsElement_WhenOnlyOneMatches()
    {
        int[] source = [1, 2, 3];
        int result = source.DefaultIfNotSingle(x => x == 2);
        Assert.Equal(2, result);
    }

    [Fact]
    public void DefaultIfNotSingle_WithPredicate_ReturnsNull_WhenMultipleElementsMatch()
    {
        int[] source = [1, 2, 2, 3];
        int result = source.DefaultIfNotSingle(x => x > 1);
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsNull_WhenEmpty()
    {
        ImmutableArray<int> source = ImmutableArray<int>.Empty;
        int result = source.DefaultIfNotSingle(x => x > 0);
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsElement_WhenSingleMatch()
    {
        ImmutableArray<int> source = [.. new[] { 5, 10, 15 }];
        int result = source.DefaultIfNotSingle(x => x == 10);
        Assert.Equal(10, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsNull_WhenMultipleMatches()
    {
        ImmutableArray<int> source = [.. new[] { 5, 10, 10, 15 }];
        int result = source.DefaultIfNotSingle(x => x > 5);
        Assert.Equal(0, result);
    }
}
