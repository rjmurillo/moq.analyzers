using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects P95-ratio regressions only when the mean-corroborated stable worse-set aggregate exceeds the 5% gate.
/// Tail-only jitter cannot trigger this gate because each P95 change must also have a mean change outside the mean noise band.
/// </summary>
public sealed class P95RatioRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc />
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
        => RegressionStrategyHelper.HasAggregateRatioRegression(
            comparison,
            logger,
            new RegressionRatioMetricConfig(
                "P95 ratio",
                GetP95Ratio,
                result => BenchmarkDotNetDiffer.GetP95Delta(result.Conclusion, result.BaseResult, result.DiffResult),
                stabilityDeltaSelector: GetStabilityDelta),
            out details);

    private static double GetStabilityDelta(RegressionResult result)
    {
        double p95Delta = BenchmarkDotNetDiffer.GetP95Delta(result.Conclusion, result.BaseResult, result.DiffResult);
        return double.IsNaN(p95Delta)
            ? double.NaN
            : BenchmarkDotNetDiffer.GetMeanDelta(result.Conclusion, result.BaseResult, result.DiffResult);
    }

    private static double GetP95Ratio(RegressionResult result)
    {
        Debug.Assert(result.BaseResult.Statistics?.Percentiles != null, "Stable P95 results have baseline percentiles.");
        Debug.Assert(result.DiffResult.Statistics?.Percentiles != null, "Stable P95 results have diff percentiles.");
        Percentiles basePercentiles = result.BaseResult.Statistics!.Percentiles!;
        Percentiles diffPercentiles = result.DiffResult.Statistics!.Percentiles!;

        return result.Conclusion == ComparisonResult.Greater
            ? basePercentiles.P95 / diffPercentiles.P95
            : diffPercentiles.P95 / basePercentiles.P95;
    }
}
