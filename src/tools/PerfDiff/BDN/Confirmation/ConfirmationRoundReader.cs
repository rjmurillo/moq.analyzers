using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// Reads paired confirmation rounds from disk.
/// </summary>
/// <remarks>
/// The layout is <c>{root}/baseline/r{N}</c> and <c>{root}/current/r{N}</c>, where N starts at 1.
/// Each round folder holds the BenchmarkDotNet JSON reports of one process.
/// </remarks>
internal static class ConfirmationRoundReader
{
    internal const string BaselineFolderName = "baseline";
    internal const string CurrentFolderName = "current";
    internal const string RoundFolderPrefix = "r";

    private const string FullBdnJsonFilePattern = "*full-compressed.json";

    /// <summary>
    /// Reads every complete round under <paramref name="roundsRoot"/>.
    /// </summary>
    /// <param name="roundsRoot">The confirmation output root.</param>
    /// <param name="logger">Receives read errors.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>The rounds in run order, or <see langword="null"/> when a round is missing or unreadable.</returns>
    internal static async Task<IReadOnlyList<ConfirmationRound>?> TryReadRoundsAsync(string roundsRoot, ILogger logger, CancellationToken cancellationToken)
    {
        if (!TryGetRoundNumbers(Path.Combine(roundsRoot, BaselineFolderName), out int[]? baselineRounds)
            || !TryGetRoundNumbers(Path.Combine(roundsRoot, CurrentFolderName), out int[]? currentRounds))
        {
            LogError(logger, $"Confirmation root '{roundsRoot}' needs '{BaselineFolderName}' and '{CurrentFolderName}' folders.");
            return null;
        }

        if (!AreNumberedFromOne(baselineRounds, currentRounds))
        {
            LogError(logger, $"Confirmation rounds must be numbered from 1 with no gaps on both sides. Baseline: [{string.Join(", ", baselineRounds)}]. Current: [{string.Join(", ", currentRounds)}].");
            return null;
        }

        List<ConfirmationRound> rounds = new(baselineRounds.Length);
        foreach (int round in baselineRounds)
        {
            ConfirmationRound? pair = await TryReadRoundAsync(roundsRoot, round, logger, cancellationToken).ConfigureAwait(false);
            if (pair is null)
            {
                return null;
            }

            rounds.Add(pair);
        }

        return rounds;
    }

    private static bool AreNumberedFromOne(int[] baselineRounds, int[] currentRounds)
        => baselineRounds.SequenceEqual(currentRounds) && baselineRounds.SequenceEqual(Enumerable.Range(1, baselineRounds.Length));

    private static async Task<ConfirmationRound?> TryReadRoundAsync(string roundsRoot, int round, ILogger logger, CancellationToken cancellationToken)
    {
        string folderName = RoundFolderPrefix + round.ToString(CultureInfo.InvariantCulture);
        Dictionary<string, Benchmark>? baseline = await TryReadRoundSideAsync(Path.Combine(roundsRoot, BaselineFolderName, folderName), logger, cancellationToken).ConfigureAwait(false);
        Dictionary<string, Benchmark>? current = await TryReadRoundSideAsync(Path.Combine(roundsRoot, CurrentFolderName, folderName), logger, cancellationToken).ConfigureAwait(false);
        return baseline is null || current is null ? null : new ConfirmationRound(baseline, current);
    }

    private static bool TryGetRoundNumbers(string sideFolder, [NotNullWhen(true)] out int[]? rounds)
    {
        if (!Directory.Exists(sideFolder))
        {
            rounds = null;
            return false;
        }

        rounds = Directory.GetDirectories(sideFolder)
            .Select(Path.GetFileName)
            .Select(static name => name is not null
                && name.StartsWith(RoundFolderPrefix, StringComparison.Ordinal)
                && int.TryParse(name.AsSpan(RoundFolderPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                    ? number
                    : -1)
            .Where(static number => number > 0)
            .Order()
            .ToArray();
        return true;
    }

    private static async Task<Dictionary<string, Benchmark>?> TryReadRoundSideAsync(string folder, ILogger logger, CancellationToken cancellationToken)
    {
        string[] files = Directory.GetFiles(folder, FullBdnJsonFilePattern, SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            LogError(logger, $"Confirmation round folder '{folder}' has no '{FullBdnJsonFilePattern}' report.");
            return null;
        }

        (bool success, BdnResult?[] results) = await BenchmarkFileReader.TryGetBdnResultAsync(files, logger, cancellationToken).ConfigureAwait(false);
        if (!success)
        {
            return null;
        }

        return BenchmarkDotNetDiffer.TryBuildBenchmarkMap(results, folder, logger, out Dictionary<string, Benchmark>? benchmarks)
            ? benchmarks
            : null;
    }

#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
#pragma warning disable CA2254 // The logging message template should not vary between calls
    private static void LogError(ILogger logger, string message) => logger.LogError(message);
#pragma warning restore CA2254
#pragma warning restore CA1848
}
