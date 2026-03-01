using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on percentile (P95) execution time thresholds.
/// </summary>
public sealed class PercentileRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        RegressionMetricConfig config = new(
            DisplayThreshold: Threshold.Parse("250ms"),
            ThresholdValueNs: 250D * TimeUnitConstants.NanoSecondsToMilliseconds,
            MetricSelector: b => b.Statistics?.Percentiles?.P95,
            DisplayValueSelector: r => r.DiffResult.Statistics!.Percentiles!.P95 / TimeUnitConstants.NanoSecondsToMilliseconds,
            MetricName: "P95");

        return RegressionStrategyHelper.HasRegression(comparison, logger, config, out details);
    }
}
