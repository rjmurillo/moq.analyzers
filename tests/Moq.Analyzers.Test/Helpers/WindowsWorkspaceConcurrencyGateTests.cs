namespace Moq.Analyzers.Test.Helpers;

public class WindowsWorkspaceConcurrencyGateTests
{
    [Fact]
    public async Task RunAsync_WithoutGate_RunsDelegate()
    {
        bool ran = false;

        await WindowsWorkspaceConcurrencyGate.RunAsync(
            () =>
            {
                ran = true;
                return Task.CompletedTask;
            },
            gate: null,
            CancellationToken.None);

        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_OnCurrentPlatform_RunsDelegate()
    {
        bool ran = false;

        await WindowsWorkspaceConcurrencyGate.RunAsync(
            () =>
            {
                ran = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_WithGate_LimitsRunsInFlight()
    {
        using SemaphoreSlim gate = new(2);
        using SemaphoreSlim release = new(0);
        int inFlight = 0;
        int maxInFlight = 0;
        int started = 0;

        async Task Run()
        {
            Interlocked.Increment(ref started);
            int current = Interlocked.Increment(ref inFlight);
            InterlockedMax(ref maxInFlight, current);
            await release.WaitAsync().ConfigureAwait(false);
            Interlocked.Decrement(ref inFlight);
        }

        Task[] runs = [.. Enumerable.Range(0, 3).Select(_ => WindowsWorkspaceConcurrencyGate.RunAsync(Run, gate, CancellationToken.None))];

        // Two runs hold both permits, so the third waits on the gate.
        Assert.Equal(2, Volatile.Read(ref started));
        Assert.Equal(0, gate.CurrentCount);

        release.Release(3);
        await Task.WhenAll(runs);

        Assert.Equal(3, Volatile.Read(ref started));
        Assert.Equal(2, maxInFlight);
        Assert.Equal(2, gate.CurrentCount);
    }

    [Fact]
    public async Task RunAsync_WhenRunThrows_ReleasesPermit()
    {
        using SemaphoreSlim gate = new(1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => WindowsWorkspaceConcurrencyGate.RunAsync(
                () => Task.FromException(new InvalidOperationException()),
                gate,
                CancellationToken.None));

        Assert.Equal(1, gate.CurrentCount);
    }

    [Fact]
    public async Task RunAsync_WhenCanceledWhileQueued_DoesNotRunDelegate()
    {
        // No permit is free, so the run waits on the gate until the token cancels the wait.
        using SemaphoreSlim gate = new(0);
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        bool ran = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => WindowsWorkspaceConcurrencyGate.RunAsync(
                () =>
                {
                    ran = true;
                    return Task.CompletedTask;
                },
                gate,
                cancellation.Token));

        Assert.False(ran);
        Assert.Equal(0, gate.CurrentCount);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current = Volatile.Read(ref target);
        while (value > current)
        {
            int previous = Interlocked.CompareExchange(ref target, value, current);
            if (previous == current)
            {
                return;
            }

            current = previous;
        }
    }
}
