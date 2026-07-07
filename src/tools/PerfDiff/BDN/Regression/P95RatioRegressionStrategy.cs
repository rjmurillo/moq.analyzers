using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects P95-ratio regressions only when the stable worse-set aggregate exceeds the 5% gate.
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
                result => BenchmarkDotNetDiffer.GetP95Delta(result.Conclusion, result.BaseResult, result.DiffResult)),
            out details);

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
