﻿namespace Moq.Analyzers.Common;

internal static class EnumerableExtensions
{
    /// <inheritdoc cref="DefaultIfNotSingle{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    public static TSource? DefaultIfNotSingle<TSource>(this IEnumerable<TSource> source)
    {
        return source.DefaultIfNotSingle(_ => true);
    }

    /// <summary>
    /// Returns the only element of a sequence that satisfies a specified condition or default if no such element exists or more than one element satisfies the condition.
    /// </summary>
    /// <typeparam name="TSource">The type of the <paramref name="source"/> collection.</typeparam>
    /// <param name="source">The collection to enumerate.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    /// The single element that satisfies the condition, or default if no such element exists or more than one element satisfies the condition.
    /// </returns>
    /// <remarks>
    /// This should be equivalent to calling <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// combined with a catch that returns <see langword="null"/>.
    /// </remarks>
    public static TSource? DefaultIfNotSingle<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
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
