using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on percentile (P95) execution time thresholds.
/// </summary>
public sealed class PercentileRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <summary>
    /// Determines whether a performance regression exists based on the 95th percentile (P95) execution time, using a fixed threshold of 250 milliseconds.
    /// </summary>
    /// <param name="comparison">An array of benchmark comparison results to evaluate.</param>
    /// <param name="logger">The logger used for diagnostic output.</param>
    /// <param name="details">Outputs detailed information about the regression detection result.</param>
    /// <returns>True if a regression is detected according to the P95 threshold; otherwise, false.</returns>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
    {
        Threshold testThreshold = Threshold.Create(ThresholdUnit.Milliseconds, 250D);
        return RegressionStrategyHelper.HasRegression(
            comparison,
            logger,
            testThreshold,
            b => b.Statistics?.Percentiles?.P95,
            r => r.DiffResult.Statistics!.Percentiles!.P95 / TimeUnitConstants.NanoSecondsToMilliseconds,
            "P95",
            out details);
    }
}
