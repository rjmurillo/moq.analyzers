using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
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
        return RegressionStrategyHelper.HasRegression(
            comparison,
            logger,
            testThreshold,
            b => b.Statistics?.Mean,
            r => r.DiffResult.Statistics!.Mean / 1_000_000D,
            "Mean",
            out details);
    }
}
