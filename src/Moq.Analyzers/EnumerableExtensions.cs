namespace Moq.Analyzers;

internal static class EnumerableExtensions
{
    /// <inheritdoc cref="SingleWhenOnly{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    public static TSource? SingleWhenOnly<TSource>(this IEnumerable<TSource> source)
    {
        return source.SingleWhenOnly(_ => true);
    }

    /// <summary>
    /// Returns the only element of a sequence that satisfies a specified condition,
    /// and returns a default value if no such element exists or there are multiple matches.
    /// </summary>
    /// <typeparam name="TSource">The type of the <paramref name="source"/> collection.`</typeparam>
    /// <param name="source">The collection to enumerate.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns><see langword="true"/> if there is only one element in the collection; <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// This should be equivalent to calling <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// combined with a catch that returns <see langword="null"/>.
    /// </remarks>
    public static TSource? SingleWhenOnly<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        bool isFound = false;
        TSource? item = default;

        foreach (TSource element in source.Where(predicate))
        {
            if (isFound)
            {
                // We already found an element, thus there's multiple matches; return default.
                return default;
            }

            isFound = true;
            item = element;
        }

        return item;
    }
}
