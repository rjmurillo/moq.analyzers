// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using DataTransferContracts;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.SignificanceTesting;

namespace PerfDiff;

public static class BenchmarkDotNetDiffer
{
    private const string FullBdnJsonFileExtension = "full-compressed.json";

    public static async Task<BenchmarkComparisonResult> TryCompareBenchmarkDotNetResultsAsync(string baselineFolder, string resultsFolder, ILogger logger)
    {
        BenchmarkComparisonService service = new(logger);
        return await service.CompareAsync(baselineFolder, resultsFolder).ConfigureAwait(false);
    }

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

        (bool baseResultsSuccess, BdnResult[] baseResults) = await BenchmarkFileReader.TryGetBdnResultAsync(baseFiles, logger).ConfigureAwait(false);
        if (!baseResultsSuccess)
        {
            return null;
        }

        (bool resultsSuccess, BdnResult[] diffResults) = await BenchmarkFileReader.TryGetBdnResultAsync(resultsFiles, logger).ConfigureAwait(false);
        if (!resultsSuccess)
        {
            return null;
        }

        Dictionary<string?, Benchmark> benchmarkIdToDiffResults = diffResults
            .SelectMany(result => result.Benchmarks)
            .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult);

        Dictionary<string?, Benchmark> benchmarkIdToBaseResults = baseResults
            .SelectMany(result => result.Benchmarks)
            .ToDictionary(benchmarkResult => benchmarkResult.FullName, benchmarkResult => benchmarkResult);

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

    // Helper methods for regression strategies
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

    public static double GetRatio(RegressionResult item)
        => GetRatio(item.Conclusion, item.BaseResult, item.DiffResult);

    public static double GetRatio(EquivalenceTestConclusion conclusion, Benchmark baseResult, Benchmark diffResult)
        => conclusion == EquivalenceTestConclusion.Faster
            ? baseResult.Statistics.Median / diffResult.Statistics.Median
            : diffResult.Statistics.Median / baseResult.Statistics.Median;
}
