using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
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
        bool hasViolation = false;

        foreach (BdnComparisonResult result in comparison)
        {
            if (result.DiffResult.Statistics?.Percentiles == null)
            {
                logger.LogWarning("test: '{Id}' diff result does not have any statistics! Unable to perform comparison.", result.Id);
                continue;
            }

            double p95Ms = result.DiffResult.Statistics.Percentiles.P95 / 1_000_000.0;
            if (p95Ms > testThreshold.GetValue([p95Ms]))
            {
                logger.LogInformation("test: '{Id}' P95 execution time {P95Ms:F2}ms exceeds threshold {ThresholdMs}ms", result.Id, p95Ms, testThreshold);
                hasViolation = true;
            }
        }

        details = new RegressionDetectionResult { Threshold = testThreshold };
        return hasViolation;
    }
}
