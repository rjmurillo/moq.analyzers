// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;
using Pragmastat;

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
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A <see cref="BenchmarkComparisonResult"/> indicating comparison success and regression detection.</returns>
    public static async Task<BenchmarkComparisonResult> TryCompareBenchmarkDotNetResultsAsync(string baselineFolder, string resultsFolder, ILogger logger, CancellationToken cancellationToken)
    {
        BenchmarkComparisonService service = new(logger);
        return await service.CompareAsync(baselineFolder, resultsFolder, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to load and match benchmark results from baseline and results folders.
    /// </summary>
    /// <param name="baselineFolder">The folder containing baseline results.</param>
    /// <param name="resultsFolder">The folder containing new results.</param>
    /// <param name="logger">Logger for reporting errors.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>An array of <see cref="BdnComparisonResult"/> if successful; otherwise, <see langword="null"/>.</returns>
    internal static async Task<BdnComparisonResult[]?> TryGetBdnResultsAsync(string baselineFolder, string resultsFolder, ILogger logger, CancellationToken cancellationToken)
    {
        if (!TryGetResultFiles(baselineFolder, resultsFolder, logger, out string[]? baseFiles, out string[]? resultsFiles))
        {
            return null;
        }

        (bool baseResultsSuccess, BdnResult?[] baseResults) = await BenchmarkFileReader.TryGetBdnResultAsync(baseFiles, logger, cancellationToken).ConfigureAwait(false);
        if (!baseResultsSuccess)
        {
            return null;
        }

        (bool resultsSuccess, BdnResult?[] diffResults) = await BenchmarkFileReader.TryGetBdnResultAsync(resultsFiles, logger, cancellationToken).ConfigureAwait(false);
        if (!resultsSuccess)
        {
            return null;
        }

        if (!TryBuildBenchmarkMap(baseResults, "baseline", logger, out Dictionary<string, Benchmark>? benchmarkIdToBaseResults)
            || !TryBuildBenchmarkMap(diffResults, "results", logger, out Dictionary<string, Benchmark>? benchmarkIdToDiffResults))
        {
            return null;
        }

        if (!HaveMatchingBenchmarkNames(benchmarkIdToBaseResults, benchmarkIdToDiffResults, logger))
        {
            return null;
        }

        return CreateComparisonResults(benchmarkIdToBaseResults, benchmarkIdToDiffResults);
    }

    private static bool TryGetResultFiles(
        string baselineFolder,
        string resultsFolder,
        ILogger logger,
        [NotNullWhen(true)] out string[]? baseFiles,
        [NotNullWhen(true)] out string[]? resultsFiles)
    {
        if (!TryGetFilesToParse(baselineFolder, out baseFiles))
        {
            logger.LogError("Provided path does NOT exist or does not contain perf results '{BaselineFolder}'", baselineFolder);
            resultsFiles = null;
            return false;
        }

        if (!TryGetFilesToParse(resultsFolder, out resultsFiles))
        {
            logger.LogError("Provided path does NOT exist or does not contain perf results '{ResultsFolder}'", resultsFolder);
            return false;
        }

        if (baseFiles.Length > 0 && resultsFiles.Length > 0)
        {
            return true;
        }

        logger.LogError("Provided paths contained no '{FileExtension}' files.", FullBdnJsonFileExtension);
        return false;
    }

    private static bool HaveMatchingBenchmarkNames(
        Dictionary<string, Benchmark> benchmarkIdToBaseResults,
        Dictionary<string, Benchmark> benchmarkIdToDiffResults,
        ILogger logger)
    {
        string[] missingFromResults = benchmarkIdToBaseResults.Keys.Except(benchmarkIdToDiffResults.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] missingFromBaseline = benchmarkIdToDiffResults.Keys.Except(benchmarkIdToBaseResults.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (missingFromResults.Length == 0 && missingFromBaseline.Length == 0)
        {
            return true;
        }

        logger.LogError(
            "Benchmark result sets do not match. Missing from results: {MissingFromResults}. Missing from baseline: {MissingFromBaseline}.",
            string.Join(", ", missingFromResults),
            string.Join(", ", missingFromBaseline));
        return false;
    }

    private static BdnComparisonResult[] CreateComparisonResults(Dictionary<string, Benchmark> benchmarkIdToBaseResults, Dictionary<string, Benchmark> benchmarkIdToDiffResults)
    {
        BdnComparisonResult[] matched = new BdnComparisonResult[benchmarkIdToBaseResults.Count];
        int index = 0;
        foreach (KeyValuePair<string, Benchmark> baseResult in benchmarkIdToBaseResults)
        {
            bool found = benchmarkIdToDiffResults.TryGetValue(baseResult.Key, out Benchmark? diffBenchmark);
            Debug.Assert(found, "Benchmark names are validated before comparison results are created.");
            matched[index] = new BdnComparisonResult(baseResult.Key, baseResult.Value, diffBenchmark!);
            index++;
        }

        Debug.Assert(index == matched.Length, "Every baseline benchmark has a matching result benchmark.");
        return matched;
    }

    internal static bool TryBuildBenchmarkMap(BdnResult?[] results, string resultSetName, ILogger logger, [NotNullWhen(true)] out Dictionary<string, Benchmark>? benchmarks)
    {
        List<Benchmark> allBenchmarks = results
            .SelectMany(result => result?.Benchmarks ?? Enumerable.Empty<Benchmark>())
            .ToList();

        if (allBenchmarks.Count == 0)
        {
            logger.LogError("The {ResultSetName} result set contained no benchmarks.", resultSetName);
            benchmarks = null;
            return false;
        }

        foreach (Benchmark benchmark in allBenchmarks)
        {
            if (!IsValidBenchmark(benchmark, resultSetName, logger))
            {
                benchmarks = null;
                return false;
            }
        }

        string[] duplicateNames = allBenchmarks
            .GroupBy(static benchmark => benchmark.FullName, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            logger.LogError("The {ResultSetName} result set contains duplicate benchmarks: {DuplicateBenchmarks}.", resultSetName, string.Join(", ", duplicateNames));
            benchmarks = null;
            return false;
        }

        benchmarks = allBenchmarks.ToDictionary(static benchmark => benchmark.FullName!, static benchmark => benchmark, StringComparer.Ordinal);
        return true;
    }

    private static bool IsValidBenchmark(Benchmark benchmark, string resultSetName, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(benchmark.FullName))
        {
            logger.LogError("The {ResultSetName} result set contains a benchmark without a FullName.", resultSetName);
            return false;
        }

        Measurement[] measurements = (benchmark.Measurements ?? [])
            .Where(static measurement => string.Equals(measurement.IterationStage, "Result", StringComparison.Ordinal))
            .ToArray();

        if (measurements.Length == 0)
        {
            logger.LogError("Benchmark '{BenchmarkName}' in the {ResultSetName} result set contains no result measurements.", benchmark.FullName, resultSetName);
            return false;
        }

        if (measurements.Length == 1)
        {
            logger.LogError("Benchmark '{BenchmarkName}' in the {ResultSetName} result set contains only one result measurement.", benchmark.FullName, resultSetName);
            return false;
        }

        if (measurements.Any(static measurement => measurement.Operations <= 0))
        {
            logger.LogError("Benchmark '{BenchmarkName}' in the {ResultSetName} result set contains a result measurement with non-positive operations.", benchmark.FullName, resultSetName);
            return false;
        }

        return true;
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
    public static RegressionResult[] FindRegressions(BdnComparisonResult[] comparison, Threshold testThreshold)
    {
        SimpleEquivalenceTest equivalenceTest = new(new MannWhitneyTest());
        List<RegressionResult> results = [];

        foreach (BdnComparisonResult result in comparison
            .Where(result => result.BaseResult.Statistics != null && result.DiffResult.Statistics != null))
        {
            double[] baseValues = result.BaseResult.GetOriginalValues();
            double[] diffValues = result.DiffResult.GetOriginalValues();
            if (baseValues.Length < 2 || diffValues.Length < 2)
            {
                continue;
            }

            ComparisonResult conclusion = equivalenceTest.Perform(
                new Sample(baseValues),
                new Sample(diffValues),
                testThreshold,
                SignificanceLevel.P05);

            if (conclusion == ComparisonResult.Indistinguishable)
            {
                continue;
            }

            results.Add(new RegressionResult(result.Id, result.BaseResult, result.DiffResult, conclusion));
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
    /// <param name="conclusion">The comparison result.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The ratio of median values.</returns>
    public static double GetMedianRatio(ComparisonResult conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null)
        {
            return double.NaN;
        }

        return conclusion == ComparisonResult.Greater
            ? baseResult.Statistics.Median / diffResult.Statistics.Median
            : diffResult.Statistics.Median / baseResult.Statistics.Median;
    }

    /// <summary>
    /// Gets the ratio of mean values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The comparison result.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The ratio of mean values.</returns>
    public static double GetMeanRatio(ComparisonResult conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null)
        {
            return double.NaN;
        }

        return conclusion == ComparisonResult.Greater
            ? baseResult.Statistics.Mean / diffResult.Statistics.Mean
            : diffResult.Statistics.Mean / baseResult.Statistics.Mean;
    }

    /// <summary>
    /// Gets the ratio of P95 values for a regression result.
    /// </summary>
    /// <param name="item">The regression result.</param>
    /// <returns>The ratio of P95 values.</returns>
    public static double GetP95Ratio(RegressionResult item)
        => GetP95Ratio(item.Conclusion, item.BaseResult, item.DiffResult);

    /// <summary>
    /// Gets the ratio of P95 values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The comparison result.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The ratio of P95 values.</returns>
    public static double GetP95Ratio(ComparisonResult conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null
            || baseResult.Statistics.Percentiles == null || diffResult.Statistics.Percentiles == null)
        {
            return double.NaN;
        }

        return conclusion == ComparisonResult.Greater
            ? baseResult.Statistics.Percentiles.P95 / diffResult.Statistics.Percentiles.P95
            : diffResult.Statistics.Percentiles.P95 / baseResult.Statistics.Percentiles.P95;
    }

    /// <summary>
    /// Gets the delta of mean values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The comparison result.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The delta of mean values.</returns>
    public static double GetMeanDelta(ComparisonResult conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null)
        {
            return double.NaN;
        }

        return conclusion == ComparisonResult.Greater
            ? baseResult.Statistics.Mean - diffResult.Statistics.Mean
            : diffResult.Statistics.Mean - baseResult.Statistics.Mean;
    }

    /// <summary>
    /// Gets the delta of P95 values for the specified benchmarks and conclusion.
    /// </summary>
    /// <param name="conclusion">The comparison result.</param>
    /// <param name="baseResult">The baseline benchmark.</param>
    /// <param name="diffResult">The diff benchmark.</param>
    /// <returns>The delta of P95 values.</returns>
    public static double GetP95Delta(ComparisonResult conclusion, Benchmark baseResult, Benchmark diffResult)
    {
        if (baseResult.Statistics == null || diffResult.Statistics == null
            || baseResult.Statistics.Percentiles == null || diffResult.Statistics.Percentiles == null)
        {
            return double.NaN;
        }

        return conclusion == ComparisonResult.Greater
            ? baseResult.Statistics.Percentiles.P95 - diffResult.Statistics.Percentiles.P95
            : diffResult.Statistics.Percentiles.P95 - baseResult.Statistics.Percentiles.P95;
    }
}
