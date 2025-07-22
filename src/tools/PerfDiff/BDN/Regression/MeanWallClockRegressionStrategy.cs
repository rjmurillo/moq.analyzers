using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on mean (average) wall clock execution time thresholds.
/// </summary>
public sealed class MeanWallClockRegressionStrategy : IBenchmarkRegressionStrategy
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
            r => r.DiffResult.Statistics!.Mean / TimeUnitConstants.NanoSecondsToMilliseconds,
            "Mean",
            out details);
    }
}
