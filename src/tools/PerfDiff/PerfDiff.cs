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
    /// <param name="request">The folders and options of this run.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="token">Token to observe for cancellation requests.</param>
    /// <returns>0 for pass, <see cref="Program.UnhandledExceptionExitCode"/> for an error, <see cref="Program.RegressionExitCode"/> for a regression.</returns>
    internal static async Task<int> CompareAsync(PerfDiffRequest request, ILogger logger, CancellationToken token)
    {
        IReadOnlySet<string>? confirmOnlyIds = null;
        if (request.ConfirmOnlyPath is not null && !RegressedIdsFile.TryRead(request.ConfirmOnlyPath, logger, out confirmOnlyIds))
        {
            return Program.UnhandledExceptionExitCode;
        }

        return await CompareAsync(
            request,
            logger,
            token,
            (baselinePath, resultsPath, compareLogger, compareToken)
                => new BenchmarkComparisonService(compareLogger).CompareAsync(baselinePath, resultsPath, compareToken, confirmOnlyIds),
            static (resultsEtlPath, baselineEtlPath) =>
            {
                bool succeeded = EtlDiffer.TryCompareETL(resultsEtlPath, baselineEtlPath, out bool regressionDetected);
                return (succeeded, regressionDetected);
            }).ConfigureAwait(false);
    }

    internal static async Task<int> CompareAsync(
        PerfDiffRequest request,
        ILogger logger,
        CancellationToken token,
        Func<string, string, ILogger, CancellationToken, Task<BenchmarkComparisonResult>> compareBenchmarksAsync,
        Func<string, string, (bool CompareSucceeded, bool RegressionDetected)> compareEtl)
    {
        token.ThrowIfCancellationRequested();

        BenchmarkComparisonResult benchmarkComparison = await compareBenchmarksAsync(request.BaselineFolder, request.ResultsFolder, logger, token).ConfigureAwait(false);
        (bool compareSucceeded, bool regressionDetected) = benchmarkComparison;

        if (!compareSucceeded)
        {
            logger.LogError("Failed to compare the performance results see log.");
            return Program.UnhandledExceptionExitCode;
        }

        if (request.RegressedOutputPath is not null && !TryWriteRegressedIds(request.RegressedOutputPath, benchmarkComparison.RegressedIds, logger))
        {
            return Program.UnhandledExceptionExitCode;
        }

        if (!regressionDetected)
        {
            logger.LogTrace("No performance regression found.");
            return 0;
        }

        return ResolveBdnRegressionExitCode(request, logger, compareEtl);
    }

    private static int ResolveBdnRegressionExitCode(
        PerfDiffRequest request,
        ILogger logger,
        Func<string, string, (bool CompareSucceeded, bool RegressionDetected)> compareEtl)
    {
        (bool etlCompareSucceeded, bool etlRegressionDetected) = CheckEltTraces(request.BaselineFolder, request.ResultsFolder, logger, compareEtl);
        if (!etlCompareSucceeded)
        {
            logger.LogWarning("We detected a regression in BenchmarkDotNet and there is no ETL info.");
            return request.FailOnRegression ? Program.RegressionExitCode : 0;
        }

        if (etlRegressionDetected)
        {
            logger.LogWarning("We detected a regression in BenchmarkDotNet and there _is_ ETL info which agrees there was a regression.");
            return request.FailOnRegression ? Program.RegressionExitCode : 0;
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
