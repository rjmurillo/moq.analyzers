using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Moq.Analyzers.Test.Common;

public class EnumerableExtensionsTests
{
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
    public void DefaultIfNotSingle_WithPredicate_ReturnsNull_WhenNoElementsMatch()
    {
        int[] source = [1, 2, 3];
        int? result = source.DefaultIfNotSingle(x => x > 10);
        Assert.Equal(0, result);
    }

    [Fact]
    public void DefaultIfNotSingle_WithPredicate_ReturnsElement_WhenOnlyOneMatches()
    {
        int[] source = [1, 2, 3];
        int? result = source.DefaultIfNotSingle(x => x == 2);
        Assert.Equal(2, result);
    }

    [Fact]
    public void DefaultIfNotSingle_WithPredicate_ReturnsNull_WhenMultipleElementsMatch()
    {
        int[] source = [1, 2, 2, 3];
        int? result = source.DefaultIfNotSingle(x => x > 1);
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
    public void DefaultIfNotSingle_StopsEnumeratingAfterSecondMatch()
    {
        CountingEnumerable<int> source = new(new[] { 1, 2, 3, 4 });
        int? result = source.DefaultIfNotSingle(x => x > 1);
        Assert.Equal(0, result);
        Assert.Equal(3, source.Count);
    }

    [Fact]
    public void DefaultIfNotSingle_String_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        IEnumerable<string> source = new List<string> { "a", "b", "c" };
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => source.DefaultIfNotSingle(null!));
        Assert.Equal("predicate", ex.ParamName);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_String_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        ImmutableArray<string> source = ImmutableArray.Create("a", "b", "c");
        Assert.Throws<ArgumentNullException>(() => source.DefaultIfNotSingle(null!));
    }

    [Fact]
    public void CountingEnumerable_Count_Resets_OnEachEnumeration()
    {
        CountingEnumerable<int> source = new CountingEnumerable<int>(new[] { 1, 2, 3 });

        using (IEnumerator<int> enumerator = source.GetEnumerator())
        {
            Assert.Equal(0, source.Count);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, source.Count);
        }

        List<int> items = new List<int>();
        foreach (int item in source)
        {
            items.Add(item);
        }

        Assert.Equal(3, source.Count);
        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_CallsEnumerableExtension()
    {
        ImmutableArray<string> source = ImmutableArray.Create("a", "b", "c");
        string? result = source.DefaultIfNotSingle(x => string.Equals(x, "b"));
        Assert.Equal("b", result);
    }

    [Fact]
    public void DefaultIfNotSingle_ThrowsArgumentNullException_WhenSourceIsNull()
    {
        IEnumerable<string>? source = null;
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => EnumerableExtensions.DefaultIfNotSingle(source!, x => true));
        Assert.Equal("source", ex.ParamName);
    }

    [Fact]
    public void DefaultIfNotSingle_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        IEnumerable<string> source = new[] { "a", "b", "c" };
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => EnumerableExtensions.DefaultIfNotSingle(source, null!));
        Assert.Equal("predicate", ex.ParamName);
    }

    private sealed class CountingEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        private readonly IEnumerable<T> _items = items;

        /// <summary>
        /// Gets tracks the number of items enumerated. Resets to 0 every time <see cref="GetEnumerator"/> is called.
        /// This means if enumeration is started but not completed, <see cref="Count"/> will reset on the next enumeration.
        /// This behavior is intentional for test scenarios that need to track enumeration per run.
        /// </summary>
        public int Count { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            // Reset count on every new enumeration. This can cause Count to reset if enumeration is started but not completed.
            Count = 0;
            foreach (T item in _items)
            {
                Count++;
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
