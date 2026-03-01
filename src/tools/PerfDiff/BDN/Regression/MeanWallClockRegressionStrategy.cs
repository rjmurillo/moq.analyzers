using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on mean (average) wall clock execution time thresholds.
/// </summary>
public sealed class MeanWallClockRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        RegressionMetricConfig config = new(
            DisplayThreshold: Threshold.Parse("100ms"),
            ThresholdValueNs: 100D * TimeUnitConstants.NanoSecondsToMilliseconds,
            MetricSelector: b => b.Statistics?.Mean,
            DisplayValueSelector: r => r.DiffResult.Statistics!.Mean / TimeUnitConstants.NanoSecondsToMilliseconds,
            MetricName: "Mean");

        return RegressionStrategyHelper.HasRegression(comparison, logger, config, out details);
    }
}
