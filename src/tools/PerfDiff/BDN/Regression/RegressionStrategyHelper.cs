using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Provides shared logic for regression strategies using <see cref="Threshold" />.
/// </summary>
public static class RegressionStrategyHelper
{
    public static bool HasRegression(
        BdnComparisonResult[] comparison,
        ILogger logger,
        Threshold testThreshold,
        Func<Benchmark, double?> metricSelector,
        Func<RegressionResult, double> displayValueSelector,
        string metricName,
        out RegressionDetectionResult details)
    {
        RegressionResult[] notSame = FindRegressions(comparison, testThreshold, metricSelector);
        List<RegressionResult> better = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Faster).OrderBy(k => k.Id).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Slower).OrderBy(k => k.Id).ToList();
        int betterCount = better.Count;
        int worseCount = worse.Count;

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double value = displayValueSelector(betterResult);
                logger.LogInformation("test: '{TestId}' {MetricName} took {MetricValue:F3} ms; better than the threshold {Threshold}", betterResult.Id, metricName, value, testThreshold);
            }

            double betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(better[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / betterCount);
            logger.LogInformation("========== {MetricName} {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", metricName, betterCount, betterGeoMean);
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double value = displayValueSelector(worseResult);
                logger.LogInformation("test: '{TestId}' {MetricName} took {MetricValue:F3} ms; worse than the threshold {Threshold}", worseResult.Id, metricName, value, testThreshold);
            }

            double worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(worse[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / worseCount);
            logger.LogInformation("========== {MetricName} {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", metricName, worseCount, worseGeoMean);
        }

        details = new RegressionDetectionResult(metricName, testThreshold);
        return worseCount > 0;
    }

    private static RegressionResult[] FindRegressions(
        BdnComparisonResult[] comparison,
        Threshold testThreshold,
        Func<Benchmark, double?> metricSelector)
    {
        List<RegressionResult> results = [];
        foreach (BdnComparisonResult result in comparison
            .Where(result => metricSelector(result.BaseResult) != null && metricSelector(result.DiffResult) != null))
        {
            double metric = metricSelector(result.DiffResult)!.Value;
            double thresholdValue = testThreshold.GetValue([metric]);

            EquivalenceTestConclusion conclusion;
            if (metric > thresholdValue)
            {
                conclusion = EquivalenceTestConclusion.Slower;
            }
            else if (metric < thresholdValue)
            {
                conclusion = EquivalenceTestConclusion.Faster;
            }
            else if (Math.Abs(metric - thresholdValue) < 0.1D)
            {
                conclusion = EquivalenceTestConclusion.Same;
            }
            else
            {
                conclusion = EquivalenceTestConclusion.Unknown;
            }

            results.Add(new RegressionResult(result.Id, result.BaseResult, result.DiffResult, conclusion));
        }

        return results.ToArray();
    }
}
