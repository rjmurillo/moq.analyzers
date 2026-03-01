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

            double betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(better[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / betterCount);
            logger.LogInformation("========== {MetricName} {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", config.MetricName, betterCount, betterGeoMean);
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double value = config.DisplayValueSelector(worseResult);
                logger.LogInformation("test: '{TestId}' {MetricName} took {MetricValue:F3} ms; worse than the threshold {Threshold}", worseResult.Id, config.MetricName, value, config.DisplayThreshold);
            }

            double worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(worse[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / worseCount);
            logger.LogInformation("========== {MetricName} {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", config.MetricName, worseCount, worseGeoMean);
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
            double metric = metricSelector(result.DiffResult)!.Value;

            ComparisonResult conclusion;
            if (metric > thresholdValueNs)
            {
                conclusion = ComparisonResult.Lesser;
            }
            else if (metric < thresholdValueNs)
            {
                conclusion = ComparisonResult.Greater;
            }
            else if (Math.Abs(metric - thresholdValueNs) < 0.1D)
            {
                conclusion = ComparisonResult.Indistinguishable;
            }
            else
            {
                conclusion = ComparisonResult.Unknown;
            }

            results.Add(new RegressionResult(result.Id, result.BaseResult, result.DiffResult, conclusion));
        }

        return results.ToArray();
    }
}
