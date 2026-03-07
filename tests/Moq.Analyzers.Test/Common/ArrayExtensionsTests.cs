namespace Moq.Analyzers.Test.Common;

public class ArrayExtensionsTests
{
    [Fact]
    public void RemoveAt_RemovesElementAtSpecifiedIndex()
    {
        int[] source = [1, 2, 3, 4, 5];
        int[] result = source.RemoveAt(2);
        Assert.Equal([1, 2, 4, 5], result);
    }

    [Fact]
    public void RemoveAt_RemovesFirstElement()
    {
        int[] source = [1, 2, 3];
        int[] result = source.RemoveAt(0);
        Assert.Equal([2, 3], result);
    }

    [Fact]
    public void RemoveAt_RemovesLastElement()
    {
        int[] source = [1, 2, 3];
        int[] result = source.RemoveAt(2);
        Assert.Equal([1, 2], result);
    }

    [Fact]
    public void RemoveAt_SingleElementArray_ReturnsEmptyArray()
    {
        string[] source = ["only"];
        string[] result = source.RemoveAt(0);
        Assert.Empty(result);
    }

    [Fact]
    public void RemoveAt_ThrowsArgumentOutOfRangeException_WhenIndexIsNegative()
    {
        int[] source = [1, 2, 3];
        Assert.Throws<ArgumentOutOfRangeException>(() => source.RemoveAt(-1));
    }

    [Fact]
    public void RemoveAt_ThrowsArgumentOutOfRangeException_WhenIndexEqualsLength()
    {
        int[] source = [1, 2, 3];
        Assert.Throws<ArgumentOutOfRangeException>(() => source.RemoveAt(3));
    }

    [Fact]
    public void RemoveAt_ThrowsArgumentOutOfRangeException_WhenIndexExceedsLength()
    {
        int[] source = [1, 2, 3];
        Assert.Throws<ArgumentOutOfRangeException>(() => source.RemoveAt(10));
    }

    [Fact]
    public void RemoveAt_DoesNotModifyOriginalArray()
    {
        int[] source = [1, 2, 3];
        _ = source.RemoveAt(1);
        Assert.Equal([1, 2, 3], source);
    }
}
