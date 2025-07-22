using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

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

        Threshold testThreshold = Threshold.Create(ThresholdUnit.Ratio, 0.35);
        RegressionResult[] notSame = BenchmarkDotNetDiffer.FindRegressions(comparison, testThreshold);

        List<RegressionResult> better = notSame.Where(result => result.Conclusion == Perfolizer.Mathematics.SignificanceTesting.EquivalenceTestConclusion.Faster).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == Perfolizer.Mathematics.SignificanceTesting.EquivalenceTestConclusion.Slower).ToList();

        // Exclude Infinity ratios
        better = better.Where(x => !double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetMedianRatio(x))).ToList();
        worse = worse.Where(x => !double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetMedianRatio(x))).ToList();

        int betterCount = better.Count;
        int worseCount = worse.Count;

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double medianRatio = BenchmarkDotNetDiffer.GetMedianRatio(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);
                logger.LogInformation("test: '{BetterId}' took '{Median:F3}' times less", betterResult.Id, medianRatio);
            }

            double betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(better[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / betterCount);
            logger.LogInformation("========== {MetricName}: {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", metricName, betterCount, betterGeoMean);
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double medianRatio = BenchmarkDotNetDiffer.GetMedianRatio(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);
                logger.LogInformation("test: '{WorseId}' took '{Median:F3}' times longer", worseResult.Id, medianRatio);
            }

            double worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(worse[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / worseCount);
            logger.LogInformation("========== {MetricName}: {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", metricName, worseCount, worseGeoMean);
        }

        details = new RegressionDetectionResult(metricName, testThreshold);
        return worseCount > 0;
    }
}
