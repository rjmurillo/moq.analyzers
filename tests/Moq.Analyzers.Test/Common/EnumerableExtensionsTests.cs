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
}
