using System.Runtime.CompilerServices;

namespace Moq.Analyzers.Benchmarks.Helpers;

internal class AsyncLazy<T> : Lazy<Task<T>>
{
    public AsyncLazy(
        Func<T> valueFactory,
        CancellationToken cancellationToken,
        TaskScheduler? scheduler = null,
        TaskCreationOptions taskCreationOptions = TaskCreationOptions.None,
        LazyThreadSafetyMode mode = LazyThreadSafetyMode.ExecutionAndPublication)
        : base(
            () =>
            Task.Factory.StartNew(
            valueFactory,
            cancellationToken,
            taskCreationOptions,
            scheduler ?? TaskScheduler.Default),
            mode)
    {
    }

    public AsyncLazy(
        Func<Task<T>> taskFactory,
        CancellationToken cancellationToken,
        TaskScheduler? scheduler = null,
        TaskCreationOptions taskCreationOptions = TaskCreationOptions.None,
        LazyThreadSafetyMode mode = LazyThreadSafetyMode.ExecutionAndPublication)
        : base(
            () =>
            Task.Factory.StartNew(
                () => taskFactory(),
                cancellationToken,
                taskCreationOptions,
                scheduler ?? TaskScheduler.Default)
            .Unwrap(),
            mode)
    {
    }

    public TaskAwaiter<T> GetAwaiter()
    {
        return Value.GetAwaiter();
    }
}
