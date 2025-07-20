using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on percentile (P95) execution time thresholds.
/// </summary>
public sealed class PercentileRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        Threshold testThreshold = Threshold.Create(ThresholdUnit.Milliseconds, 250D);
        return RegressionStrategyHelper.HasRegression(
            comparison,
            logger,
            testThreshold,
            b => b.Statistics?.Percentiles?.P95,
            r => r.DiffResult.Statistics!.Percentiles!.P95 / 1_000_000D,
            "P95",
            out details);
    }
}
