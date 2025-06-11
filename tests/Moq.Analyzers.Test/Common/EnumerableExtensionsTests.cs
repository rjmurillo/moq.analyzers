using System;
using System.Collections.Generic;
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
    public void DefaultIfNotSingle_IEnumerable_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        IEnumerable<string> source = new[] { "a", "b", "c" };
        try
        {
            source.DefaultIfNotSingle(null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.Equal("predicate", ex.ParamName);
        }
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        System.Collections.Immutable.ImmutableArray<int> source = System.Collections.Immutable.ImmutableArray.Create(1, 2, 3);
        try
        {
            source.DefaultIfNotSingle(null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.Equal("predicate", ex.ParamName);
        }
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsNull_WhenIsDefaultOrEmpty()
    {
        System.Collections.Immutable.ImmutableArray<int> source = System.Collections.Immutable.ImmutableArray<int>.Empty;
        int? result = source.DefaultIfNotSingle(x => true);
        Assert.Equal(0, result);

        System.Collections.Immutable.ImmutableArray<int> defaultSource = default(System.Collections.Immutable.ImmutableArray<int>);
        result = defaultSource.DefaultIfNotSingle(x => true);
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsElement_WhenSingleMatch()
    {
        System.Collections.Immutable.ImmutableArray<int> source = System.Collections.Immutable.ImmutableArray.Create(1, 2, 3);
        int? result = source.DefaultIfNotSingle(x => x == 2);
        Assert.Equal(2, result);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ReturnsNull_WhenMultipleMatches()
    {
        System.Collections.Immutable.ImmutableArray<int> source = System.Collections.Immutable.ImmutableArray.Create(1, 2, 2, 3);
        int? result = source.DefaultIfNotSingle(x => x == 2);
        Assert.Equal(0, result);
    }
}
