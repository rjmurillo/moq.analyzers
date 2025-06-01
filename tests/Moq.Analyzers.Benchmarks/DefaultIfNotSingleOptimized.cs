using System.Collections.Immutable;
using System.Linq;

namespace Moq.Analyzers.Benchmarks;

#pragma warning disable ECS0900 // Minimize boxing and unboxing
internal static class DefaultIfNotSingleOptimized
{
    public static T? DefaultIfNotSingleOptimizedMethod<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        bool found = false;
        T? item = default;
        foreach (T element in source)
        {
            if (!predicate(element))
            {
                continue;
            }

            if (found)
            {
                return default;
            }

            found = true;
            item = element;
        }

        return item;
    }

    public static T? DefaultIfNotSingleOptimizedMethod<T>(this IEnumerable<T> source)
        => source.DefaultIfNotSingleOptimizedMethod(static _ => true);

    public static T? DefaultIfNotSingleOptimizedMethod<T>(this ImmutableArray<T> source, Func<T, bool> predicate)
        => source.AsEnumerable().DefaultIfNotSingleOptimizedMethod(predicate);
#pragma warning restore ECS0900
}
