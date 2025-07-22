using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on mean (average) wall clock execution time thresholds.
/// </summary>
public sealed class MeanWallClockRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <summary>
    /// Determines whether a performance regression has occurred based on the mean wall clock execution time, using a fixed threshold of 100 milliseconds.
    /// </summary>
    /// <param name="comparison">An array of benchmark comparison results to evaluate.</param>
    /// <param name="logger">The logger used for reporting regression analysis details.</param>
    /// <param name="details">Outputs detailed information about the regression detection result.</param>
    /// <returns>True if a regression is detected according to the mean execution time threshold; otherwise, false.</returns>
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
