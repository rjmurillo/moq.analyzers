using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on average execution time thresholds.
/// </summary>
public class AverageRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        Threshold testThreshold = Threshold.Create(ThresholdUnit.Milliseconds, 100D);
        RegressionResult[] notSame = FindMeanRegressions(comparison, testThreshold);
        List<RegressionResult> better = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Faster).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Slower).ToList();
        int betterCount = better.Count;
        int worseCount = worse.Count;

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double mean = betterResult.DiffResult.Statistics!.Mean / 1_000_000D;
                logger.LogInformation("test: '{TestId}' took {Mean:F3} ms; better than the threshold {Threshold}", betterResult.Id, mean, testThreshold);
            }
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double mean = worseResult.DiffResult.Statistics!.Mean / 1_000_000D;
                logger.LogInformation("test: '{TestId}' took {Mean:F3} ms; worse than the threshold {Threshold}", worseResult.Id, mean, testThreshold);
            }
        }

        details = new RegressionDetectionResult { Threshold = testThreshold };
        return worseCount > 0;
    }

    private static RegressionResult[] FindMeanRegressions(BdnComparisonResult[] comparison, Threshold testThreshold)
    {
        List<RegressionResult> results = [];
        foreach (BdnComparisonResult result in comparison
                     .Where(result => result.BaseResult.Statistics != null && result.DiffResult.Statistics != null))
        {
            double diffAvgMs = result.DiffResult.Statistics!.Mean;
            double thresholdValue = testThreshold.GetValue([diffAvgMs]);

            EquivalenceTestConclusion conclusion;
            if (diffAvgMs > thresholdValue)
            {
                conclusion = EquivalenceTestConclusion.Slower;
            }
            else if (diffAvgMs < thresholdValue)
            {
                conclusion = EquivalenceTestConclusion.Faster;
            }
            else if (Math.Abs(diffAvgMs - thresholdValue) < 0.1D)
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
