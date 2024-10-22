using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace Moq.Analyzers.Common;

internal static class ArrayExtensions
{
    /// <summary>
    /// Returns an array with the element at the specified position removed.
    /// </summary>
    /// <typeparam name="T">The array type.</typeparam>
    /// <param name="array">The array.</param>
    /// <param name="index">The 0-based index into the array for the element to omit from the returned array.</param>
    /// <returns>The new array.</returns>
    internal static T[] RemoveAt<T>(this T[] array, int index)
    {
        return RemoveRange(array, index, 1);
    }

    /// <summary>
    /// Returns an array with the elements at the specified position removed.
    /// </summary>
    /// <typeparam name="T">The array type.</typeparam>
    /// <param name="array">The array.</param>
    /// <param name="index">The 0-based index into the array for the element to omit from the returned array.</param>
    /// <param name="length">The number of elements to remove.</param>
    /// <returns>The new array.</returns>
    private static T[] RemoveRange<T>(this T[] array, int index, int length)
    {
        // Range check
        if (index < 0 || index >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (length < 0 || index + length > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

#pragma warning disable S2583 // Change condition so it doesn't always evaluate to false
        if (array.Length == 0)
#pragma warning restore S2583
        {
            return array;
        }

        T[] tmp = new T[array.Length - length];
        Array.Copy(array, tmp, index);
        Array.Copy(array, index + length, tmp, index, array.Length - index - length);

        return tmp;
    }
}
