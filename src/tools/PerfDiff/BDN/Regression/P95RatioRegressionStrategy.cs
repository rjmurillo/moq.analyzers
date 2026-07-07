using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

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
                BenchmarkDotNetDiffer.GetP95Ratio,
                result => BenchmarkDotNetDiffer.GetP95Delta(result.Conclusion, result.BaseResult, result.DiffResult)),
            out details);
}
