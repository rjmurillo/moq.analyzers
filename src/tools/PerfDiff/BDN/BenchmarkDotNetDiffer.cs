// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using DataTransferContracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff;

public static class BenchmarkDotNetDiffer
{
    private const string FullBdnJsonFileExtension = "full-compressed.json";

    public static async Task<BenchmarkComparisonResult> TryCompareBenchmarkDotNetResultsAsync(string baselineFolder, string resultsFolder, ILogger logger)
    {
        // search folder for benchmark dotnet results
        BdnComparisonResult[]? comparison = await TryGetBdnResultsAsync(baselineFolder, resultsFolder, logger).ConfigureAwait(false);
        if (comparison is null)
        {
            return new BenchmarkComparisonResult(false, false);
        }

        bool percentRegression = HasPercentageRegression(comparison, logger, out Threshold percentThreshold);
        bool avgRegression = HasAvgRegression(comparison, logger, out List<string> avgViolations);
        bool p99Regression = HasP99Regression(comparison, logger, out List<string> p99Violations);

        if (!percentRegression && !avgRegression && !p99Regression)
        {
            logger.LogInformation("All analyzers are within the average, P99, and percentage-based thresholds. No regressions detected.");
            return new BenchmarkComparisonResult(true, false);
        }

        if (avgRegression)
        {
            foreach (string msg in avgViolations)
            {
                logger.LogError(msg);
            }
        }

        if (p99Regression)
        {
            foreach (string msg in p99Violations)
            {
                logger.LogError(msg);
            }
        }

        if (percentRegression)
        {
            logger.LogError("Percentage-based regression detected (threshold: {PercentThreshold}).", percentThreshold);
        }

        return new BenchmarkComparisonResult(true, true);
    }

    private static RegressionResult[] FindRegressions(BdnComparisonResult[] comparison, Threshold testThreshold)
    {
        List<RegressionResult> results = [];
        foreach (var result in comparison
            .Where(result => result.BaseResult.Statistics != null && result.DiffResult.Statistics != null)) // failures
        {
            double[]? baseValues = result.BaseResult.GetOriginalValues();
            double[]? diffValues = result.DiffResult.GetOriginalValues();

            TostResult<MannWhitneyResult>? userTresholdResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, testThreshold);
            if (userTresholdResult.Conclusion == EquivalenceTestConclusion.Same)
                continue;

            results.Add(new RegressionResult(result.Id, result.BaseResult, result.DiffResult, userTresholdResult.Conclusion));
        }

        return results.ToArray();
    }

    private static double GetRatio(RegressionResult item)
        => GetRatio(item.Conclusion, item.BaseResult, item.DiffResult);

    private static double GetRatio(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
        => conclusion == EquivalenceTestConclusion.Faster
            ? baseResult.Statistics.Median / diffResult.Statistics.Median
            : diffResult.Statistics.Median / baseResult.Statistics.Median;

    private static bool HasAvgRegression(BdnComparisonResult[] comparison, ILogger logger, out List<string> violations)
    {
        const double analyzerAvgThresholdMs = 100.0;
        violations = [];

        foreach (var result in comparison)
        {
            if (result.DiffResult.Statistics == null)
            {
                continue;
            }

            double avgMs = result.DiffResult.Statistics.Mean;
            if (avgMs > analyzerAvgThresholdMs)
            {
                logger.LogInformation("test: '{Id}' average execution time {AvgMs:F2}ms exceeds threshold {D}ms", result.Id, avgMs, analyzerAvgThresholdMs);
                violations.Add($"Analyzer '{result.Id}' average execution time {avgMs:F2}ms exceeds threshold {analyzerAvgThresholdMs}ms.");
            }
        }

        return violations.Count > 0;
    }

    private static bool HasP99Regression(BdnComparisonResult[] comparison, ILogger logger, out List<string> violations)
    {
        const double analyzerP99ThresholdMs = 250.0;
        violations = [];

        foreach (var result in comparison)
        {
            if (result.DiffResult.Statistics == null)
            {
                continue;
            }

            double p95Ms = result.DiffResult.Statistics.Percentiles.P95;
            if (p95Ms > analyzerP99ThresholdMs)
            {
                logger.LogInformation("test: '{Id}' P99 execution time {P99Ms:F2}ms exceeds threshold {D}ms", result.Id, p95Ms, analyzerP99ThresholdMs);
                violations.Add($"Analyzer '{result.Id}' P99 execution time {p95Ms:F2}ms exceeds threshold {analyzerP99ThresholdMs}ms.");
            }
        }

        return violations.Count > 0;
    }

    private static bool HasPercentageRegression(BdnComparisonResult[] comparison, ILogger logger, out Threshold testThreshold)
    {
        _ = Threshold.TryParse("35%", out testThreshold);
        RegressionResult[] notSame = FindRegressions(comparison, testThreshold);

        List<RegressionResult> better = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Faster).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Slower).ToList();
        int betterCount = better.Count;
        int worseCount = worse.Count;

        // Exclude Infinity ratios
        better = better.Where(x => !double.IsPositiveInfinity(GetRatio(x))).ToList();
        worse = worse.Where(x => !double.IsPositiveInfinity(GetRatio(x))).ToList();

        if (betterCount > 0)
        {
            double betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(GetRatio(better[0])), (x, y) => x + Math.Log10(GetRatio(y))) / betterCount);
            logger.LogInformation("better: {BetterCount}, geomean: {BetterGeoMean:F3}%", betterCount, betterGeoMean);
            foreach (var betterResult in better)
            {
                double mean = GetRatio(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);
                logger.LogInformation("test: '{BetterId}' tool '{Mean:F3}' times less", betterResult.Id, mean);
            }
        }

        if (worseCount > 0)
        {
            double worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(GetRatio(worse[0])), (x, y) => x + Math.Log10(GetRatio(y))) / worseCount);
            logger.LogInformation("worse: {WorseCount}, geomean: {WorseGeoMean:F3}%", worseCount, worseGeoMean);
            foreach (var worseResult in worse)
            {
                double mean = GetRatio(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);
                logger.LogInformation("test: '{WorseId}' took '{Mean:F3}' times longer", worseResult.Id, mean);
            }
        }

        return worseCount > 0;
    }

    private static async Task<BdnResult?> ReadFromFileAsync(string resultFilePath, ILogger logger)
    {
        try
        {
            return JsonConvert.DeserializeObject<BdnResult>(await File.ReadAllTextAsync(resultFilePath).ConfigureAwait(false));
        }
        catch (JsonSerializationException)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning disable CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            logger.LogError($"Exception while reading the {resultFilePath} file.");
#pragma warning restore CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            return null;
        }
    }

    private static async Task<BdnResults> TryGetBdnResultAsync(string[] paths, ILogger logger)
    {
        BdnResult?[] results = await Task.WhenAll(paths.Select(path => ReadFromFileAsync(path, logger))).ConfigureAwait(false);
        return new BdnResults(!results.Any(x => x is null), results);
    }

    private static async Task<BdnComparisonResult[]?> TryGetBdnResultsAsync(
                string baselineFolder,
                string resultsFolder,
                ILogger logger)
    {
        if (!TryGetFilesToParse(baselineFolder, out string[]? baseFiles))
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning disable CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            logger.LogError($"Provided path does NOT exist or does not contain perf results '{baselineFolder}'");
#pragma warning restore CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            return null;
        }

        if (!TryGetFilesToParse(resultsFolder, out string[]? resultsFiles))
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning disable CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            logger.LogError($"Provided path does NOT exist or does not contain perf results '{resultsFolder}'");
#pragma warning restore CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            return null;
        }

        if (!baseFiles.Any() || !resultsFiles.Any())
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            logger.LogError($"Provided paths contained no '{FullBdnJsonFileExtension}' files.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            return null;
        }

        (bool baseResultsSuccess, BdnResult[] baseResults) = await TryGetBdnResultAsync(baseFiles, logger).ConfigureAwait(false);
        if (!baseResultsSuccess)
        {
            return null;
        }

        (bool resultsSuccess, BdnResult[] diffResults) = await TryGetBdnResultAsync(resultsFiles, logger).ConfigureAwait(false);
        if (!resultsSuccess)
        {
            return null;
        }

        Dictionary<string, Benchmark> benchmarkIdToDiffResults = diffResults
            .SelectMany(result => result.Benchmarks)
            .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult);

        Dictionary<string, Benchmark> benchmarkIdToBaseResults = baseResults
            .SelectMany(result => result.Benchmarks)
            .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult); // we use ToDictionary to make sure the results have unique IDs

        return benchmarkIdToBaseResults
            .Where(baseResult => benchmarkIdToDiffResults.ContainsKey(baseResult.Key))
            .Select(baseResult => new BdnComparisonResult(baseResult.Key, baseResult.Value, benchmarkIdToDiffResults[baseResult.Key]))
            .ToArray();
    }

    private static bool TryGetFilesToParse(string path, [NotNullWhen(true)] out string[]? files)
    {
        if (Directory.Exists(path))
        {
            files = Directory.GetFiles(path, $"*{FullBdnJsonFileExtension}", SearchOption.AllDirectories);
            return true;
        }

        if (File.Exists(path) || !path.EndsWith(FullBdnJsonFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            files = [path];
            return true;
        }

        files = null;
        return false;
    }
}
