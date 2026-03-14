// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace PerfDiff.Tests;

/// <summary>
/// Verifies CancellationToken propagation through the async call chain.
/// </summary>
public class CancellationTests
{
    /// <summary>
    /// A pre-cancelled token passed to CompareAsync must not be silently swallowed.
    /// CompareAsync calls token.ThrowIfCancellationRequested() at entry.
    /// The exception propagates as OperationCanceledException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task CompareAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => PerfDiff.CompareAsync(
                    baselineFolder: tempDir,
                    resultsFolder: tempDir,
                    failOnRegression: false,
                    logger: NullLogger.Instance,
                    token: cts.Token));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Program.RunAsync catches OperationCanceledException and returns exit code 1.
    /// This is the expected contract: the CLI tool does not crash on cancellation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task RunAsync_PreCancelledToken_ReturnsUnhandledExceptionExitCode()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            int exitCode = await Program.RunAsync(
                baseline: tempDir,
                results: tempDir,
                verbosity: null,
                failOnRegression: false,
                cancellationToken: cts.Token);

            Assert.Equal(Program.UnhandledExceptionExitCode, exitCode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Cancellation token is forwarded through BenchmarkFileReader.TryGetBdnResultAsync
    /// all the way to File.ReadAllTextAsync. A pre-cancelled token causes the read to throw.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task BenchmarkFileReader_CancelledToken_DoesNotSwallowCancellation()
    {
        using CancellationTokenSource cts = new();

        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        // Write a valid-looking filename so the file list is non-empty.
        string fakeFile = Path.Combine(tempDir, "results.full-compressed.json");
        await File.WriteAllTextAsync(fakeFile, "{}", CancellationToken.None);

        await cts.CancelAsync();

        try
        {
            // TryGetBdnResultAsync will attempt File.ReadAllTextAsync with the cancelled token.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => BDN.BenchmarkFileReader.TryGetBdnResultAsync(
                    paths: [fakeFile],
                    logger: NullLogger.Instance,
                    cancellationToken: cts.Token));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
