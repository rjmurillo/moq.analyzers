// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.ETL;

namespace PerfDiff;

public static class PerfDiff
{
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

        (bool etlCompareSucceeded, bool etlRegressionDetected) = CheckEltTraces(baselineFolder, resultsFolder);
        if (!etlCompareSucceeded)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            logger.LogTrace("We detected a regression in BenchmarkDotNet and there is no ETL info.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            return failOnRegression ? 1 : 0;
        }

        if (etlRegressionDetected)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            logger.LogTrace("We detected a regression in BenchmarkDotNet and there _is_ ETL info which agrees there was a regression.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
            return failOnRegression ? 1 : 0;
        }

#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
        logger.LogTrace("We detected a regression in BenchmarkDotNet but examining the ETL trace determined that it was noise.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogTrace(ILogger, string?, params object?[])'
        return 0;
    }

    private static (bool compareSucceeded, bool regressionDetected) CheckEltTraces(string baselineFolder, string resultsFolder)
    {
        // try look for ETL traces
        if (!TryGetETLPaths(baselineFolder, out string? baselineEtlPath))
        {
            return (false, false);
        }

        if (!TryGetETLPaths(resultsFolder, out string? resultsEtlPath))
        {
            return (false, false);
        }

        // Compare ETL
        if (!EtlDiffer.TryCompareETL(resultsEtlPath, baselineEtlPath, out bool regressionDetected))
        {
            return (false, false);
        }

        return (true, regressionDetected);
    }

    private const string ETLFileExtension = "etl.zip";

    private static bool TryGetETLPaths(string path, [NotNullWhen(true)] out string? etlPath)
    {
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, $"*{ETLFileExtension}", SearchOption.AllDirectories);
            etlPath = files.SingleOrDefault();
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
