using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

public class MeanPercentageRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc />
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        const string metricName = "Mean Ratio";

        // There are two thresholds here: one is a relative 5%, the other is an absolute 0.5ms. Both must be exceeded to trigger a regression.
        Threshold relativeThreshold = Threshold.Create(ThresholdUnit.Ratio, 0.05);
        Threshold absoluteThreshold = Threshold.Create(ThresholdUnit.Milliseconds, 0.5D);

        double absoluteThresholdValue = absoluteThreshold.GetValue([0]);

        RegressionResult[] notSame = BenchmarkDotNetDiffer.FindRegressions(comparison, relativeThreshold);

        List<RegressionResult> better = notSame.Where(result => result.Conclusion == Perfolizer.Mathematics.SignificanceTesting.EquivalenceTestConclusion.Faster).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == Perfolizer.Mathematics.SignificanceTesting.EquivalenceTestConclusion.Slower).ToList();
        int betterCount = better.Count;
        int betterCountExceedingThreshold = 0;
        int worseCount = worse.Count;
        int worseCountExceedingThreshold = 0;

        // Exclude Infinity ratios
        better = better.Where(x => !double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetMedianRatio(x))).ToList();
        worse = worse.Where(x => !double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetMedianRatio(x))).ToList();

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double mean = BenchmarkDotNetDiffer.GetMeanRatio(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);
                double delta = BenchmarkDotNetDiffer.GetMeanDelta(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);

                if (delta <= absoluteThresholdValue)
                {
                    continue;
                }

                betterCountExceedingThreshold++;
                double deltaMs = delta / 1_000_000;

                logger.LogInformation(
                    "test: '{BetterId}' took {Mean:F3} times ({Delta:F2} ms) less",
                    betterResult.Id,
                    mean,
                    deltaMs);
            }

            if (betterCountExceedingThreshold > 0)
            {
                double betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(better[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / betterCount);
                logger.LogInformation("========== {MetricName}: {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", metricName, betterCountExceedingThreshold, betterGeoMean);
            }
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double mean = BenchmarkDotNetDiffer.GetMeanRatio(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);
                double delta = BenchmarkDotNetDiffer.GetMeanDelta(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);

                if (delta >= absoluteThresholdValue)
                {
                    continue;
                }

                worseCountExceedingThreshold++;
                double deltaMs = delta / 1_000_000;

                logger.LogInformation(
                    "test: '{WorseId}' took {Mean:F3} times ({Delta:F2} ms) more",
                    worseResult.Id,
                    mean,
                    deltaMs);
            }

            if (worseCountExceedingThreshold > 0)
            {
                double worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(worse[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetMedianRatio(y))) / worseCount);
                logger.LogInformation("========== {MetricName}: {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", metricName, worseCountExceedingThreshold, worseGeoMean);
            }
        }

        details = new RegressionDetectionResult(metricName, relativeThreshold);
        return worseCountExceedingThreshold > 0;
    }
}
