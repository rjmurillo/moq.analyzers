using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on percentage thresholds between benchmark results.
/// </summary>
public sealed class PercentageRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        const string metricName = "Median ratio";

        Threshold testThreshold = Threshold.Parse("35%");
        RegressionResult[] notSame = BenchmarkDotNetDiffer.FindRegressions(comparison, testThreshold);

        List<RegressionResult> better = notSame.Where(result => result.Conclusion == ComparisonResult.Greater).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == ComparisonResult.Lesser).ToList();

        int betterCount = better.Count;
        int worseCount = worse.Count;

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double medianRatio = BenchmarkDotNetDiffer.GetMedianRatio(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);
                logger.LogInformation("test: '{BetterId}' took '{Median:F3}' times less", betterResult.Id, medianRatio);
            }

            if (RegressionStrategyHelper.TryGetGeometricMean(better, BenchmarkDotNetDiffer.GetMedianRatio, out double betterGeoMean))
            {
                logger.LogInformation("========== {MetricName}: {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", metricName, betterCount, betterGeoMean);
            }
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double medianRatio = BenchmarkDotNetDiffer.GetMedianRatio(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);
                logger.LogInformation("test: '{WorseId}' took '{Median:F3}' times longer", worseResult.Id, medianRatio);
            }

            if (RegressionStrategyHelper.TryGetGeometricMean(worse, BenchmarkDotNetDiffer.GetMedianRatio, out double worseGeoMean))
            {
                logger.LogInformation("========== {MetricName}: {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", metricName, worseCount, worseGeoMean);
            }
        }

        details = new RegressionDetectionResult(metricName, testThreshold);
        return worseCount > 0;
    }
}
