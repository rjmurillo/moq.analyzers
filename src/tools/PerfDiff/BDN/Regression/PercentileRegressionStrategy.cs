using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on percentile (P95) execution time thresholds.
/// </summary>
public class PercentileRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        Threshold testThreshold = Threshold.Create(ThresholdUnit.Milliseconds, 250D);
        RegressionResult[] notSame = FindP95Regressions(comparison, testThreshold);
        List<RegressionResult> better = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Faster).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == EquivalenceTestConclusion.Slower).ToList();
        int betterCount = better.Count;
        int worseCount = worse.Count;

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double p95 = betterResult.DiffResult.Statistics!.Percentiles!.P95 / 1_000_000D;
                logger.LogInformation("test: '{TestId}' took {P95:F3} ms; better than the threshold {Threshold}", betterResult.Id, p95, testThreshold);
            }
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double p95 = worseResult.DiffResult.Statistics!.Percentiles!.P95 / 1_000_000D;
                logger.LogInformation("test: '{TestId}' took {P95:F3} ms; worse than the threshold {Threshold}", worseResult.Id, p95, testThreshold);
            }
        }

        details = new RegressionDetectionResult { Threshold = testThreshold };
        return worseCount > 0;
    }

    private static RegressionResult[] FindP95Regressions(BdnComparisonResult[] comparison, Threshold testThreshold)
    {
        List<RegressionResult> results = [];
        foreach (BdnComparisonResult result in comparison
                     .Where(result => result.BaseResult.Statistics is { Percentiles: not null }
                                      && result.DiffResult.Statistics is { Percentiles: not null }))
        {
            double p95 = result.DiffResult.Statistics!.Percentiles!.P95;
            double thresholdValue = testThreshold.GetValue([p95]);

            EquivalenceTestConclusion conclusion;
            if (p95 > thresholdValue)
            {
                conclusion = EquivalenceTestConclusion.Slower;
            }
            else if (p95 < thresholdValue)
            {
                conclusion = EquivalenceTestConclusion.Faster;
            }
            else if (Math.Abs(p95 - thresholdValue) < 0.1D)
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
