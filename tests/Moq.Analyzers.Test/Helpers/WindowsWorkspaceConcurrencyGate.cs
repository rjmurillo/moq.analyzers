namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// Limits how many verifier runs are in flight at once on Windows.
/// </summary>
/// <remarks>
/// <para>
/// On Windows, Roslyn Workspaces stores document text in 8 MB memory mapped files
/// (<c>TemporaryStorageService</c>). Each test workspace gets its own service, so each one holds at least one block
/// while the test runs. The blocks count against the system commit limit.
/// </para>
/// <para>
/// The parallel test framework starts every test case in a class at once. In CI, hundreds of workspaces were alive
/// together, and their blocks reached 32 GB. The commit limit ran out, and the test host crashed with a stack
/// overflow when it started a thread. Other platforms use in-memory storage, so this gate does nothing there.
/// </para>
/// </remarks>
internal static class WindowsWorkspaceConcurrencyGate
{
    // Twice the 4 cores of a hosted Windows runner keeps the CPU busy while some runs wait on I/O.
    private static readonly int MaxRunsInFlight = 8;

    private static SemaphoreSlim Gate { get; } = new(MaxRunsInFlight);

    /// <summary>
    /// Runs <paramref name="run"/>. On Windows, it first waits until fewer than
    /// <see cref="MaxRunsInFlight"/> verifier runs are in flight.
    /// </summary>
    /// <param name="run">The verifier run.</param>
    /// <param name="cancellationToken">The token that cancels the wait.</param>
    /// <returns>A task that completes when <paramref name="run"/> completes.</returns>
    public static async Task RunAsync(Func<Task> run, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            await run().ConfigureAwait(false);
            return;
        }

        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await run().ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }
}
