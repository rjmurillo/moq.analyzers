using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on percentile (P95) execution time thresholds.
/// </summary>
public class PercentileRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out object details)
    {
        const double thresholdMs = 250.0;
        List<string> violations = new();

        foreach (BdnComparisonResult result in comparison)
        {
            if (result.DiffResult.Statistics?.Percentiles == null)
            {
                logger.LogWarning("test: '{Id}' diff result does not have any statistics! Unable to perform comparison.", result.Id);
                continue;
            }

            // Convert nanoseconds to milliseconds
            double p95Ms = result.DiffResult.Statistics.Percentiles.P95 / 1_000_000.0;
            if (p95Ms > thresholdMs)
            {
                logger.LogInformation("test: '{Id}' P95 execution time {P95Ms:F2}ms exceeds threshold {D}ms", result.Id, p95Ms, thresholdMs);
                violations.Add($"Analyzer '{result.Id}' P95 execution time {p95Ms:F2}ms exceeds threshold {thresholdMs}ms.");
            }
        }

        details = violations;
        return violations.Count > 0;
    }
}
