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
        Threshold displayThreshold = Threshold.Parse("100ms");
        double thresholdValueNs = 100D * TimeUnitConstants.NanoSecondsToMilliseconds;
        return RegressionStrategyHelper.HasRegression(
            comparison,
            logger,
            displayThreshold,
            thresholdValueNs,
            b => b.Statistics?.Mean,
            r => r.DiffResult.Statistics!.Mean / TimeUnitConstants.NanoSecondsToMilliseconds,
            "Mean",
            out details);
    }
}
