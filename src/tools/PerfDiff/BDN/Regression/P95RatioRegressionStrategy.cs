using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

public sealed class P95RatioRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc />
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        const string metricName = "P95 ratio";

        // There are two thresholds here: one is a relative 5%, the other is an absolute 0.5ms. Both must be exceeded to trigger a regression.
        Threshold relativeThreshold = Threshold.Parse("5%");
        double absoluteThresholdValueNs = 0.5D * TimeUnitConstants.NanoSecondsToMilliseconds;

        RegressionResult[] notSame = BenchmarkDotNetDiffer.FindRegressions(comparison, relativeThreshold);

        List<RegressionResult> better = notSame.Where(result => result.Conclusion == ComparisonResult.Greater).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == ComparisonResult.Lesser).ToList();
        int betterCountExceedingThreshold = 0;
        int worseCountExceedingThreshold = 0;
        List<RegressionResult> betterExceedingThreshold = [];
        List<RegressionResult> worseExceedingThreshold = [];

        if (better.Count > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double medianRatio = BenchmarkDotNetDiffer.GetMedianRatio(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);
                double p95Delta = BenchmarkDotNetDiffer.GetP95Delta(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);

                if (p95Delta <= absoluteThresholdValueNs)
                {
                    continue;
                }

                betterCountExceedingThreshold++;
                betterExceedingThreshold.Add(betterResult);
                double deltaMs = p95Delta / TimeUnitConstants.NanoSecondsToMilliseconds;

                logger.LogInformation(
                    "test: '{BetterId}' took {Mean:F3} times ({Delta:F2} ms) less",
                    betterResult.Id,
                    medianRatio,
                    deltaMs);
            }

            if (betterCountExceedingThreshold > 0
                && RegressionStrategyHelper.TryGetGeometricMean(betterExceedingThreshold, BenchmarkDotNetDiffer.GetMedianRatio, out double betterGeoMean))
            {
                logger.LogInformation("========== {MetricName}: {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", metricName, betterCountExceedingThreshold, betterGeoMean);
            }
        }

        if (worse.Count > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double medianRatio = BenchmarkDotNetDiffer.GetMedianRatio(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);
                double p95Delta = BenchmarkDotNetDiffer.GetP95Delta(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);

                if (p95Delta <= absoluteThresholdValueNs)
                {
                    continue;
                }

                worseCountExceedingThreshold++;
                worseExceedingThreshold.Add(worseResult);
                double deltaMs = p95Delta / TimeUnitConstants.NanoSecondsToMilliseconds;

                logger.LogInformation(
                    "test: '{WorseId}' took {Mean:F3} times ({Delta:F2} ms) more",
                    worseResult.Id,
                    medianRatio,
                    deltaMs);
            }

            if (worseCountExceedingThreshold > 0
                && RegressionStrategyHelper.TryGetGeometricMean(worseExceedingThreshold, BenchmarkDotNetDiffer.GetMedianRatio, out double worseGeoMean))
            {
                logger.LogInformation("========== {MetricName}: {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", metricName, worseCountExceedingThreshold, worseGeoMean);
            }
        }

        details = new RegressionDetectionResult(metricName, relativeThreshold);
        return worseCountExceedingThreshold > 0;
    }
}
