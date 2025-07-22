// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.SignificanceTesting;

namespace PerfDiff.BDN;

/// <summary>
/// Provides methods for comparing BenchmarkDotNet results and detecting regressions.
/// </summary>
public static class BenchmarkDotNetDiffer
{
    private const string FullBdnJsonFileExtension = "full-compressed.json";

    /// <summary>
    /// Compares two sets of BenchmarkDotNet results asynchronously.
    /// </summary>
    /// <param name="baselineFolder">The folder containing baseline results.</param>
    /// <param name="resultsFolder">The folder containing new results.</param>
    /// <param name="logger">Logger for reporting errors.</param>
    /// <returns>A <see cref="BenchmarkComparisonResult"/> indicating comparison success and regression detection.</returns>
    public static async Task<BenchmarkComparisonResult> TryCompareBenchmarkDotNetResultsAsync(string baselineFolder, string resultsFolder, ILogger logger)
    {
        BenchmarkComparisonService service = new(logger);
        return await service.CompareAsync(baselineFolder, resultsFolder).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to load and match benchmark results from baseline and results folders.
    /// </summary>
    /// <param name="baselineFolder">The folder containing baseline results.</param>
    /// <param name="resultsFolder">The folder containing new results.</param>
    /// <param name="logger">Logger for reporting errors.</param>
    /// <returns>An array of <see cref="BdnComparisonResult"/> if successful; otherwise, <see langword="null"/>.</returns>
    internal static async Task<BdnComparisonResult[]?> TryGetBdnResultsAsync(string baselineFolder, string resultsFolder, ILogger logger)
    {
        if (!TryGetFilesToParse(baselineFolder, out string[]? baseFiles))
        {
            logger.LogError("Provided path does NOT exist or does not contain perf results '{BaselineFolder}'", baselineFolder);
            return null;
        }

        if (!TryGetFilesToParse(resultsFolder, out string[]? resultsFiles))
        {
            logger.LogError("Provided path does NOT exist or does not contain perf results '{ResultsFolder}'", resultsFolder);
            return null;
        }

        if (!baseFiles.Any() || !resultsFiles.Any())
        {
            logger.LogError($"Provided paths contained no '{FullBdnJsonFileExtension}' files.");
            return null;
        }

        (bool baseResultsSuccess, BdnResult?[] baseResults) = await BenchmarkFileReader.TryGetBdnResultAsync(baseFiles, logger).ConfigureAwait(false);
        if (!baseResultsSuccess)
        {
            return null;
        }

        (bool resultsSuccess, BdnResult?[] diffResults) = await BenchmarkFileReader.TryGetBdnResultAsync(resultsFiles, logger).ConfigureAwait(false);
        if (!resultsSuccess)
        {
            return null;
        }

        Dictionary<string, Benchmark> benchmarkIdToDiffResults = diffResults
            .SelectMany(result => result?.Benchmarks ?? Enumerable.Empty<Benchmark>())
            .ToDictionary(benchmarkResult => benchmarkResult.FullName ?? $"Unknown-{Guid.NewGuid():N}", benchmarkResult => benchmarkResult);

        Dictionary<string, Benchmark> benchmarkIdToBaseResults = baseResults
            .SelectMany(result => result?.Benchmarks ?? Enumerable.Empty<Benchmark>())
            .ToDictionary(benchmarkResult => benchmarkResult.FullName ?? $"Unknown-{Guid.NewGuid():N}", benchmarkResult => benchmarkResult);

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

        if (File.Exists(path) && path.EndsWith(FullBdnJsonFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            files = [path];
            return true;
        }

        files = null;
        return false;
    }

    /// <summary>
    /// Finds regressions between two sets of benchmark results using the specified threshold.
    /// </summary>
    /// <param name="comparison">Array of comparison results.</param>
    /// <param name="testThreshold">Threshold for regression detection.</param>
    /// <returns>An array of <see cref="RegressionResult"/> representing detected regressions.</returns>
    public static RegressionResult[] FindRegressions(BdnComparisonResult[] comparison, Perfolizer.Mathematics.Thresholds.Threshold testThreshold)
    {
        List<RegressionResult> results = [];
        foreach (BdnComparisonResult result in comparison
            .Where(result => result.BaseResult.Statistics != null && result.DiffResult.Statistics != null))
        {
            double[] baseValues = result.BaseResult.GetOriginalValues();
            double[] diffValues = result.DiffResult.GetOriginalValues();

            TostResult<MannWhitneyResult>? userThresholdResult = StatisticalTestHelper.CalculateTost(MannWhitneyTest.Instance, baseValues, diffValues, testThreshold);

            if (userThresholdResult.Conclusion == EquivalenceTestConclusion.Same)
            {
                continue;
            }

            results.Add(new RegressionResult(result.Id, result.BaseResult, result.DiffResult, userThresholdResult.Conclusion));
        }

        return results.ToArray();
    }

    /// <summary>
    /// Gets the ratio of median values for a regression result.
    /// </summary>
    /// <param name="item">The regression result.</param>
    /// <returns>The ratio of median values.</returns>
    public static double GetMedianRatio(RegressionResult item)
        => GetMedianRatio(item.Conclusion, item.BaseResult, item.DiffResult);

    /// <summary>
    /// Gets the ratio of median values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The equivalence test conclusion.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The ratio of median values.</returns>
    public static double GetMedianRatio(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null)
        {
            return double.NaN;
        }

        return conclusion == EquivalenceTestConclusion.Faster
            ? baseResult.Statistics.Median / diffResult.Statistics.Median
            : diffResult.Statistics.Median / baseResult.Statistics.Median;
    }

    /// <summary>
    /// Gets the ratio of mean values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The equivalence test conclusion.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The ratio of mean values.</returns>
    public static double GetMeanRatio(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null)
        {
            return double.NaN;
        }

        return conclusion == EquivalenceTestConclusion.Faster
            ? baseResult.Statistics.Mean / diffResult.Statistics.Mean
            : diffResult.Statistics.Mean / baseResult.Statistics.Mean;
    }

    /// <summary>
    /// Gets the delta of mean values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The equivalence test conclusion.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The delta of mean values.</returns>
    public static double GetMeanDelta(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null)
        {
            return double.NaN;
        }

        return conclusion == EquivalenceTestConclusion.Faster
            ? baseResult.Statistics.Mean - diffResult.Statistics.Mean
            : diffResult.Statistics.Mean - baseResult.Statistics.Mean;
    }

    /// <summary>
    /// Gets the delta of P95 values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The equivalence test conclusion.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The delta of P95 values.</returns>
    public static double GetP95Delta(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null
            || baseResult.Statistics.Percentiles == null || diffResult.Statistics.Percentiles == null)
        {
            return double.NaN;
        }

        return conclusion == EquivalenceTestConclusion.Faster
            ? baseResult.Statistics.Percentiles.P95 - diffResult.Statistics.Percentiles.P95
            : diffResult.Statistics.Percentiles.P95 - baseResult.Statistics.Percentiles.P95;
    }
}
