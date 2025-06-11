using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Moq.Analyzers.Test.Common;

public class EnumerableExtensionsTests
{
    [Fact]
    public void DefaultIfNotSingle_ReturnsNull_WhenSourceIsNull()
    {
        IEnumerable<object> source = null!;
        object? result = source.DefaultIfNotSingle();
        Assert.Null(result);
    }

    [Fact]
    public void DefaultIfNotSingle_ReturnsNull_WhenSourceIsEmpty()
    {
        IEnumerable<object> source = [];
        object? result = source.DefaultIfNotSingle();
        Assert.Null(result);
    }

    [Fact]
    public void DefaultIfNotSingle_ReturnsElement_WhenSourceContainsSingleElement()
    {
        int[] source = [42];
        int? result = source.DefaultIfNotSingle();
        Assert.Equal(42, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ReturnsNull_WhenSourceContainsMultipleElements()
    {
        int[] source = [1, 2, 3];
        int? result = source.DefaultIfNotSingle();
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsNull_WhenEmpty()
    {
        ImmutableArray<int> source = ImmutableArray<int>.Empty;
        int? result = source.DefaultIfNotSingle(x => x > 0);
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsElement_WhenSingleMatch()
    {
        ImmutableArray<int> source = [.. new[] { 5, 10, 15 }];
        int? result = source.DefaultIfNotSingle(x => x == 10);
        Assert.Equal(10, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsNull_WhenMultipleMatches()
    {
        ImmutableArray<int> source = [.. new[] { 5, 10, 10, 15 }];
        int? result = source.DefaultIfNotSingle(x => x > 5);
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_String_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        ImmutableArray<string> source = ImmutableArray.Create("a", "b", "c");
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => source.DefaultIfNotSingle(null!));
        Assert.Equal("predicate", ex.ParamName);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_CallsEnumerableExtension()
    {
        ImmutableArray<string> source = ImmutableArray.Create("a", "b", "c");
        string? result = source.DefaultIfNotSingle(x => string.Equals(x, "b"));
        Assert.Equal("b", result);
    }
}
