// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.ETL;

namespace PerfDiff;

internal static class PerfDiff
{
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
#pragma warning disable CA2254 // The logging message template should not vary between calls
    /// <summary>
    /// Compares two result folders with the BenchmarkDotNet regression gates.
    /// </summary>
    /// <param name="baselineFolder">Folder with the baseline results.</param>
    /// <param name="resultsFolder">Folder with the results under test.</param>
    /// <param name="failOnRegression">When <see langword="true"/>, a regression returns <see cref="Program.RegressionExitCode"/>.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="token">Token to observe for cancellation requests.</param>
    /// <param name="regressedOutputPath">
    /// When set, the ids of the benchmarks that tripped a gate are written there as a JSON array.
    /// The file is written on every successful comparison, as <c>[]</c> when no gate tripped.
    /// </param>
    /// <param name="confirmOnlyPath">
    /// When set, only benchmarks listed in this file (written by <paramref name="regressedOutputPath"/> in a
    /// first round) are gated. A missing or malformed file is an error, never a silent pass.
    /// </param>
    /// <returns>0 for pass, <see cref="Program.UnhandledExceptionExitCode"/> for an error, <see cref="Program.RegressionExitCode"/> for a regression.</returns>
    internal static async Task<int> CompareAsync(
        string baselineFolder,
        string resultsFolder,
        bool failOnRegression,
        ILogger logger,
        CancellationToken token,
        string? regressedOutputPath = null,
        string? confirmOnlyPath = null)
    {
        IReadOnlySet<string>? confirmOnlyIds = null;
        if (confirmOnlyPath is not null && !RegressedIdsFile.TryRead(confirmOnlyPath, logger, out confirmOnlyIds))
        {
            return Program.UnhandledExceptionExitCode;
        }

        return await CompareAsync(
            baselineFolder,
            resultsFolder,
            failOnRegression,
            logger,
            token,
            (baselinePath, resultsPath, compareLogger, compareToken)
                => new BenchmarkComparisonService(compareLogger).CompareAsync(baselinePath, resultsPath, compareToken, confirmOnlyIds),
            static (resultsEtlPath, baselineEtlPath) =>
            {
                bool succeeded = EtlDiffer.TryCompareETL(resultsEtlPath, baselineEtlPath, out bool regressionDetected);
                return (succeeded, regressionDetected);
            },
            regressedOutputPath).ConfigureAwait(false);
    }

    internal static async Task<int> CompareAsync(
        string baselineFolder,
        string resultsFolder,
        bool failOnRegression,
        ILogger logger,
        CancellationToken token,
        Func<string, string, ILogger, CancellationToken, Task<BenchmarkComparisonResult>> compareBenchmarksAsync,
        Func<string, string, (bool CompareSucceeded, bool RegressionDetected)> compareEtl,
        string? regressedOutputPath = null)
    {
        token.ThrowIfCancellationRequested();

        BenchmarkComparisonResult benchmarkComparison = await compareBenchmarksAsync(baselineFolder, resultsFolder, logger, token).ConfigureAwait(false);
        (bool compareSucceeded, bool regressionDetected) = benchmarkComparison;

        if (!compareSucceeded)
        {
            logger.LogError("Failed to compare the performance results see log.");
            return Program.UnhandledExceptionExitCode;
        }

        if (regressedOutputPath is not null && !TryWriteRegressedIds(regressedOutputPath, benchmarkComparison.RegressedIds, logger))
        {
            return Program.UnhandledExceptionExitCode;
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
            return failOnRegression ? Program.RegressionExitCode : 0;
        }

        if (etlRegressionDetected)
        {
            logger.LogWarning("We detected a regression in BenchmarkDotNet and there _is_ ETL info which agrees there was a regression.");
            return failOnRegression ? Program.RegressionExitCode : 0;
        }

        logger.LogTrace("We detected a regression in BenchmarkDotNet but examining the ETL trace determined that it was noise.");
        return 0;
    }

    private static bool TryWriteRegressedIds(string path, IReadOnlyList<string> ids, ILogger logger)
    {
        try
        {
            RegressedIdsFile.Write(path, ids);
            logger.LogInformation("Wrote {Count} regressed benchmark ids to '{Path}'.", ids.Count, path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError("Cannot write the regressed benchmark ids to '{Path}': {Reason}", path, ex.Message);
            return false;
        }
    }
#pragma warning restore CA2254
#pragma warning restore CA1848

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
