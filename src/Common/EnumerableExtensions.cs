﻿using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers.Common;

internal static class EnumerableExtensions
{
    /// <inheritdoc cref="DefaultIfNotSingle{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    public static TSource? DefaultIfNotSingle<TSource>(this IEnumerable<TSource> source)
    {
        return source.DefaultIfNotSingle(static _ => true);
    }

    /// <inheritdoc cref="DefaultIfNotSingle{TSource}(ImmutableArray{TSource}, Func{TSource, bool})"/>
    public static TSource? DefaultIfNotSingle<TSource>(this ImmutableArray<TSource> source)
    {
        return source.DefaultIfNotSingle(static _ => true);
    }

    /// <inheritdoc cref="DefaultIfNotSingle{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// <param name="source">The collection to enumerate.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    [SuppressMessage("Performance", "ECS0900:Minimize boxing and unboxing", Justification = "Should revisit. Suppressing for now to unblock refactor.")]
    public static TSource? DefaultIfNotSingle<TSource>(this ImmutableArray<TSource> source, Func<TSource, bool> predicate)
    {
        return source.AsEnumerable().DefaultIfNotSingle(predicate);
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

    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source)
        where TSource : class
    {
        return source.Where(item => item is not null)!;
    }

    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source)
        where TSource : struct
    {
        return source.Where(item => item.HasValue).Select(item => item!.Value);
    }
}
