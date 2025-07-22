// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.ETL;

namespace PerfDiff;

public static class PerfDiff
{
    /// <summary>
    /// Compares performance benchmark results between a baseline and a new results folder, optionally verifying regressions using ETL trace data.
    /// </summary>
    /// <param name="baselineFolder">The path to the baseline benchmark results folder.</param>
    /// <param name="resultsFolder">The path to the new benchmark results folder to compare against the baseline.</param>
    /// <param name="failOnRegression">If true, the method returns a failure code when a regression is confirmed.</param>
    /// <param name="token">A cancellation token to abort the operation if requested.</param>
    /// <returns>Returns 0 if no regression is detected or if a detected regression is determined to be noise; returns 1 if a regression is confirmed or if comparison fails.</returns>
    public static async Task<int> CompareAsync(
        string baselineFolder, string resultsFolder, bool failOnRegression, ILogger logger, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        (bool compareSucceeded, bool regressionDetected) = await BenchmarkDotNetDiffer.TryCompareBenchmarkDotNetResultsAsync(baselineFolder, resultsFolder, logger).ConfigureAwait(false);

        if (!compareSucceeded)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            logger.LogError("Failed to compare the performance results see log.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            return 1;
        }

        if (!regressionDetected)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            logger.LogTrace("No performance regression found.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            return 0;
        }

        (bool etlCompareSucceeded, bool etlRegressionDetected) = CheckEltTraces(baselineFolder, resultsFolder, failOnRegression);
        if (!etlCompareSucceeded)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            logger.LogTrace("We detected a regression in BenchmarkDotNet and there is no ETL info.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            return 1;
        }

        if (etlRegressionDetected)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            logger.LogTrace(" We detected a regression in BenchmarkDotNet and there _is_ ETL info which agrees there was a regression.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            return 1;
        }

#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
        logger.LogTrace("We detected a regression in BenchmarkDotNet but examining the ETL trace determined that is was noise.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
        return 0;
    }

    /// <summary>
    /// Compares ETL trace files between the baseline and results folders to detect performance regressions.
    /// </summary>
    /// <param name="baselineFolder">The path to the baseline folder containing ETL trace files.</param>
    /// <param name="resultsFolder">The path to the results folder containing ETL trace files.</param>
    /// <param name="failOnRegression">Indicates whether to treat detected regressions as failures.</param>
    /// <returns>
    /// A tuple where the first value indicates if the ETL comparison succeeded, and the second value indicates if a regression was detected.
    /// </returns>
    private static (bool compareSucceeded, bool regressionDetected) CheckEltTraces(string baselineFolder, string resultsFolder, bool failOnRegression)
    {
        bool regressionDetected = false;

        // try look for ETL traces
        if (!TryGetETLPaths(baselineFolder, out string? baselineEtlPath))
        {
            return (false, regressionDetected);
        }

        if (!TryGetETLPaths(resultsFolder, out string? resultsEtlPath))
        {
            return (false, regressionDetected);
        }

        // Compare ETL
        if (!EtlDiffer.TryCompareETL(resultsEtlPath, baselineEtlPath, out regressionDetected))
        {
            return (false, regressionDetected);
        }

        if (regressionDetected && failOnRegression)
        {
            return (true, regressionDetected);
        }

        return (false, regressionDetected);
    }

    private const string ETLFileExtension = "etl.zip";

    /// <summary>
    /// Attempts to locate a single ETL trace file with the ".etl.zip" extension in the specified path.
    /// </summary>
    /// <param name="path">The directory or file path to search for an ETL trace file.</param>
    /// <param name="etlPath">
    /// When this method returns true, contains the full path to the found ETL trace file; otherwise, null.
    /// </param>
    /// <returns>
    /// True if exactly one ETL trace file is found; otherwise, false.
    /// </returns>
    private static bool TryGetETLPaths(string path, [NotNullWhen(true)] out string? etlPath)
    {
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, $"*{ETLFileExtension}", SearchOption.AllDirectories);
            etlPath = files.SingleOrDefault();
            if (etlPath is null)
            {
                etlPath = null;
                return false;
            }

            return true;
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
