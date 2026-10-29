// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN;
using PerfDiff.BDN.Confirmation;
using PerfDiff.BDN.DataContracts;
using PerfDiff.ETL;

namespace PerfDiff;

internal static class PerfDiff
{
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
#pragma warning disable CA2254 // The logging message template should not vary between calls
    internal static async Task<int> CompareAsync(
        string baselineFolder, string resultsFolder, bool failOnRegression, ILogger logger, CancellationToken token)
        => await CompareAsync(
            baselineFolder,
            resultsFolder,
            failOnRegression,
            logger,
            token,
            CompareBenchmarks(flaggedOutputPath: null),
            CompareEtl).ConfigureAwait(false);

    internal static async Task<int> CompareAsync(
        string baselineFolder,
        string resultsFolder,
        bool failOnRegression,
        ILogger logger,
        CancellationToken token,
        Func<string, string, ILogger, CancellationToken, Task<BenchmarkComparisonResult>> compareBenchmarksAsync,
        Func<string, string, (bool CompareSucceeded, bool RegressionDetected)> compareEtl)
    {
        token.ThrowIfCancellationRequested();

        BenchmarkComparisonResult comparison = await compareBenchmarksAsync(baselineFolder, resultsFolder, logger, token).ConfigureAwait(false);
        (bool compareSucceeded, bool regressionDetected) = (comparison.CompareSucceeded, comparison.RegressionDetected);

        if (!compareSucceeded)
        {
            logger.LogError("Failed to compare the performance results see log.");
            return 1;
        }

        if (!regressionDetected)
        {
            logger.LogTrace("No performance regression found.");
            return 0;
        }

        (bool etlCompareSucceeded, bool etlRegressionDetected) = CheckEltTraces(baselineFolder, resultsFolder, logger, compareEtl);
        if (!etlCompareSucceeded)
        {
            logger.LogWarning("We detected a regression in BenchmarkDotNet and there is no ETL info.");
            return failOnRegression ? 1 : 0;
        }

        if (etlRegressionDetected)
        {
            logger.LogWarning("We detected a regression in BenchmarkDotNet and there _is_ ETL info which agrees there was a regression.");
            return failOnRegression ? 1 : 0;
        }

        logger.LogTrace("We detected a regression in BenchmarkDotNet but examining the ETL trace determined that it was noise.");
        return 0;
    }
#pragma warning restore CA2254
#pragma warning restore CA1848

    /// <summary>
    /// Gets the BenchmarkDotNet comparison. When <paramref name="flaggedOutputPath"/> is set,
    /// the comparison also writes the flagged benchmarks there after it succeeds.
    /// </summary>
    /// <param name="flaggedOutputPath">The flagged benchmark file, or <see langword="null"/> for none.</param>
    /// <returns>The comparison delegate.</returns>
    internal static Func<string, string, ILogger, CancellationToken, Task<BenchmarkComparisonResult>> CompareBenchmarks(string? flaggedOutputPath)
    {
        if (flaggedOutputPath is null)
        {
            return BenchmarkDotNetDiffer.TryCompareBenchmarkDotNetResultsAsync;
        }

        return async (baselineFolder, resultsFolder, logger, token) =>
        {
            BenchmarkComparisonResult comparison = await BenchmarkDotNetDiffer.TryCompareBenchmarkDotNetResultsAsync(baselineFolder, resultsFolder, logger, token).ConfigureAwait(false);

            // The file exists only after a successful comparison, so a caller can tell a
            // regression exit code from a comparison failure.
            if (comparison.CompareSucceeded)
            {
                await ConfirmationHypothesisFile.WriteAsync(flaggedOutputPath, comparison.FlaggedOrEmpty, token).ConfigureAwait(false);
            }

            return comparison;
        };
    }

    internal static (bool CompareSucceeded, bool RegressionDetected) CompareEtl(string resultsEtlPath, string baselineEtlPath)
    {
        bool succeeded = EtlDiffer.TryCompareETL(resultsEtlPath, baselineEtlPath, out bool regressionDetected);
        return (succeeded, regressionDetected);
    }

    private static (bool compareSucceeded, bool regressionDetected) CheckEltTraces(
        string baselineFolder,
        string resultsFolder,
        ILogger logger,
        Func<string, string, (bool CompareSucceeded, bool RegressionDetected)> compareEtl)
    {
        // try look for ETL traces
        if (!TryGetETLPaths(baselineFolder, logger, out string? baselineEtlPath))
        {
            return (false, false);
        }

        if (!TryGetETLPaths(resultsFolder, logger, out string? resultsEtlPath))
        {
            return (false, false);
        }

        // Compare ETL
        return compareEtl(resultsEtlPath, baselineEtlPath);
    }

    private const string ETLFileExtension = "etl.zip";

    private static bool TryGetETLPaths(string path, ILogger logger, [NotNullWhen(true)] out string? etlPath)
    {
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, $"*{ETLFileExtension}", SearchOption.AllDirectories);
            if (files.Length > 1)
            {
                logger.LogWarning("Found {Count} ETL files in '{Path}'; using first match.", files.Length, path);
            }

            etlPath = files.FirstOrDefault();
            return etlPath is not null;
        }
        else if (File.Exists(path) && path.EndsWith(ETLFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            etlPath = path;
            return true;
        }

        etlPath = null;
        return false;
    }
}
