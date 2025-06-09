using System;
using System.Collections.Immutable;
using System.Linq;

namespace Moq.Analyzers.Benchmarks;

#pragma warning disable ECS0900 // Minimize boxing and unboxing
internal static class DefaultIfNotSingleBaseline
{
    public static T? DefaultIfNotSingleBaselineMethod<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        bool found = false;
        T? item = default;
        foreach (T element in source.Where(predicate))
        {
            if (found)
            {
                return default;
            }

            found = true;
            item = element;
        }

        return item;
    }

    public static T? DefaultIfNotSingleBaselineMethod<T>(this IEnumerable<T> source)
        => source.DefaultIfNotSingleBaselineMethod(static _ => true);

    public static T? DefaultIfNotSingleBaselineMethod<T>(this ImmutableArray<T> source, Func<T, bool> predicate)
        => source.AsEnumerable().DefaultIfNotSingleBaselineMethod(predicate);
#pragma warning restore ECS0900
}
