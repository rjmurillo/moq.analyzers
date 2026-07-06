using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on mean performance ratio thresholds, requiring both
/// a relative threshold (5% ratio) and absolute threshold (0.5ms) to be exceeded.
/// </summary>
public sealed class MeanPercentageRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc />
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        const string metricName = "Mean Ratio";

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
                double meanRatio = BenchmarkDotNetDiffer.GetMeanRatio(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);
                double meanDelta = BenchmarkDotNetDiffer.GetMeanDelta(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);

                if (meanDelta <= absoluteThresholdValueNs)
                {
                    continue;
                }

                betterCountExceedingThreshold++;
                betterExceedingThreshold.Add(betterResult);
                double deltaMs = meanDelta / TimeUnitConstants.NanoSecondsToMilliseconds;

                logger.LogInformation(
                    "test: '{BetterId}' took {Mean:F3} times ({Delta:F2} ms) less",
                    betterResult.Id,
                    meanRatio,
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
                double meanRatio = BenchmarkDotNetDiffer.GetMeanRatio(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);
                double meanDelta = BenchmarkDotNetDiffer.GetMeanDelta(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);

                if (meanDelta <= absoluteThresholdValueNs)
                {
                    continue;
                }

                worseCountExceedingThreshold++;
                worseExceedingThreshold.Add(worseResult);
                double deltaMs = meanDelta / TimeUnitConstants.NanoSecondsToMilliseconds;

                logger.LogInformation(
                    "test: '{WorseId}' took {Mean:F3} times ({Delta:F2} ms) more",
                    worseResult.Id,
                    meanRatio,
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
