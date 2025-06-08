using System.Collections;

namespace Moq.Analyzers.Test.Common;

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

    [Fact]
    public void DefaultIfNotSingle_StopsEnumeratingAfterSecondMatch()
    {
        CountingEnumerable<int> source = new(new[] { 1, 2, 3, 4 });
        int result = source.DefaultIfNotSingle(x => x > 1);

        Assert.Equal(0, result);
        Assert.Equal(3, source.Count);
    }

    [Fact]
    public void DefaultIfNotSingle_ThrowsArgumentNullException_WhenSourceIsNull()
    {
        IEnumerable<int> source = null!;
        Assert.Throws<ArgumentNullException>(() => source.DefaultIfNotSingle(x => true));
    }

    [Fact]
    public void DefaultIfNotSingle_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        IEnumerable<int> source = new List<int> { 1, 2, 3 };
        var ex = Assert.Throws<ArgumentNullException>(() => source.DefaultIfNotSingle(null!));
        Assert.Equal("predicate", ex.ParamName);
    }

    [Fact]
    public void DefaultIfNotSingle_ImmutableArray_ThrowsArgumentNullException_WhenPredicateIsNull()
    {
        var source = ImmutableArray.Create(1, 2, 3);
        Assert.Throws<ArgumentNullException>(() => source.DefaultIfNotSingle(null!));
    }

    private sealed class CountingEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        private readonly IEnumerable<T> _items = items;

        public int Count { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
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
