using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Provides shared logic for regression strategies using <see cref="Threshold" />.
/// </summary>
public static class RegressionStrategyHelper
{
    public static bool HasRegression(
        BdnComparisonResult[] comparison,
        ILogger logger,
        RegressionMetricConfig config,
        out RegressionDetectionResult details)
    {
        RegressionResult[] notSame = FindRegressions(comparison, config.ThresholdValueNs, config.MetricSelector);
        List<RegressionResult> better = notSame.Where(result => result.Conclusion == ComparisonResult.Greater).OrderBy(k => k.Id).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == ComparisonResult.Lesser).OrderBy(k => k.Id).ToList();
        int betterCount = better.Count;
        int worseCount = worse.Count;

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double value = config.DisplayValueSelector(betterResult);
                logger.LogInformation("test: '{TestId}' {MetricName} took {MetricValue:F3} ms; better than the threshold {Threshold}", betterResult.Id, config.MetricName, value, config.DisplayThreshold);
            }

            if (TryGetGeometricMean(better, BenchmarkDotNetDiffer.GetMedianRatio, out double betterGeoMean))
            {
                logger.LogInformation("========== {MetricName} {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", config.MetricName, betterCount, betterGeoMean);
            }
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double value = config.DisplayValueSelector(worseResult);
                logger.LogInformation("test: '{TestId}' {MetricName} took {MetricValue:F3} ms; worse than the threshold {Threshold}", worseResult.Id, config.MetricName, value, config.DisplayThreshold);
            }

            if (TryGetGeometricMean(worse, BenchmarkDotNetDiffer.GetMedianRatio, out double worseGeoMean))
            {
                logger.LogInformation("========== {MetricName} {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", config.MetricName, worseCount, worseGeoMean);
            }
        }

        details = new RegressionDetectionResult(config.MetricName, config.DisplayThreshold);
        return worseCount > 0;
    }

    private static RegressionResult[] FindRegressions(
        BdnComparisonResult[] comparison,
        double thresholdValueNs,
        Func<Benchmark, double?> metricSelector)
    {
        List<RegressionResult> results = [];
        foreach (BdnComparisonResult result in comparison
            .Where(result => metricSelector(result.BaseResult) != null && metricSelector(result.DiffResult) != null))
        {
            double baselineMetric = metricSelector(result.BaseResult)!.Value;
            double metric = metricSelector(result.DiffResult)!.Value;

            ComparisonResult conclusion;
            if (metric > thresholdValueNs && baselineMetric <= thresholdValueNs)
            {
                conclusion = ComparisonResult.Lesser;
            }
            else if (metric <= thresholdValueNs && baselineMetric > thresholdValueNs)
            {
                conclusion = ComparisonResult.Greater;
            }
            else
            {
                conclusion = ComparisonResult.Indistinguishable;
            }

            if (conclusion != ComparisonResult.Indistinguishable)
            {
                results.Add(new RegressionResult(result.Id, result.BaseResult, result.DiffResult, conclusion));
            }
        }

        return results.ToArray();
    }

    internal static bool TryGetGeometricMean(IEnumerable<RegressionResult> results, Func<RegressionResult, double> ratioSelector, out double geometricMean)
    {
        int count = 0;
        double logSum = 0;

        foreach (RegressionResult result in results)
        {
            double ratio = ratioSelector(result);
            Debug.Assert(ratio >= 0 || double.IsNaN(ratio), "Benchmark ratios are non-negative unless undefined.");
            if (double.IsNaN(ratio))
            {
                // 0 ns baseline and 0 ns diff produces 0/0. It has no ratio to aggregate.
                continue;
            }

            logSum += Math.Log10(ratio);
            count++;
        }

        if (count == 0)
        {
            geometricMean = double.NaN;
            return false;
        }

        Debug.Assert(count > 0, "At least one numeric ratio is required before dividing.");
        geometricMean = Math.Pow(10, logSum / count);
        return true;
    }
}
