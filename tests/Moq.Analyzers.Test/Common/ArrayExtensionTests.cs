namespace Moq.Analyzers.Test.Common;

public class ArrayExtensionTests
{
    [Fact]
    public void RemoveAt_RemovesElementAtIndex()
    {
        // Arrange
        int[] actual = [1, 2, 3, 4, 5];
        int[] expected = [1, 2, 4, 5];
        const int indexToRemove = 2;

        // Act
        int[] result = actual.RemoveAt(indexToRemove);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveAt_FirstElement_RemovesCorrectly()
    {
        // Arrange
        int[] input = [1, 2, 3];
        int[] expected = [2, 3];

        // Act
        int[] result = input.RemoveAt(0);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveAt_LastElement_RemovesCorrectly()
    {
        // Arrange
        int[] input = [1, 2, 3];
        int[] expected = [1, 2];

        // Act
        int[] result = input.RemoveAt(input.Length - 1);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveAt_SingleElementArray_ReturnsEmptyArray()
    {
        // Arrange
        int[] input = [42];

        // Act
        int[] result = input.RemoveAt(0);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void RemoveAt_IndexOutOfRange_ThrowsException()
    {
        // Arrange
        int[] input = [1, 2, 3];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => input.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => input.RemoveAt(3));
    }

    [Fact]
    public void RemoveAt_EmptyArray_ThrowsException()
    {
        // Arrange
        int[] input = [];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => input.RemoveAt(0));
    }
}
