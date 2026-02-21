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
        Threshold displayThreshold = Threshold.Parse("250ms");
        double thresholdValueNs = 250D * TimeUnitConstants.NanoSecondsToMilliseconds;
        return RegressionStrategyHelper.HasRegression(
            comparison,
            logger,
            displayThreshold,
            thresholdValueNs,
            b => b.Statistics?.Percentiles?.P95,
            r => r.DiffResult.Statistics!.Percentiles!.P95 / TimeUnitConstants.NanoSecondsToMilliseconds,
            "P95",
            out details);
    }
}
