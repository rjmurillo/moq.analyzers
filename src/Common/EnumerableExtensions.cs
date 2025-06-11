using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Moq.Analyzers.Common;

internal static class EnumerableExtensions
{
    /// <inheritdoc cref="DefaultIfNotSingle{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    internal static TSource? DefaultIfNotSingle<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            return default;
        }

        return source.DefaultIfNotSingle(static _ => true);
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
    /// combined with a catch that returns <see langword="default"/>.
    /// </remarks>
    internal static TSource? DefaultIfNotSingle<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source is ImmutableArray<TSource> immutableArray)
        {
            return DefaultIfNotSingle(immutableArray, predicate);
        }

        if (source == null)
        {
            return default;
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        bool isFound = false;
        TSource? item = default;

        foreach (TSource element in source)
        {
            if (!predicate(element))
            {
                continue;
            }

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
    internal static TSource? DefaultIfNotSingle<TSource>(this ImmutableArray<TSource> source, Func<TSource, bool> predicate)
    {
        if (source.IsDefaultOrEmpty)
        {
            return default;
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        bool found = false;
        TSource? item = default;

        for (int i = 0; i < source.Length; i++)
        {
            TSource element = source[i];
            if (!predicate(element))
            {
                continue;
            }

            if (found)
            {
                // Multiple matches found; return default.
                return default;
            }

            found = true;
            item = element;
        }

        return item;
    }
}
