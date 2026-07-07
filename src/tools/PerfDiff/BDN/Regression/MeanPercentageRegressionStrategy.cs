using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects mean-ratio regressions only when the stable worse-set aggregate exceeds the 5% gate.
/// </summary>
public sealed class MeanPercentageRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc />
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
        => RegressionStrategyHelper.HasAggregateRatioRegression(
            comparison,
            logger,
            new RegressionRatioMetricConfig(
                "Mean Ratio",
                result => BenchmarkDotNetDiffer.GetMeanRatio(result.Conclusion, result.BaseResult, result.DiffResult),
                result => BenchmarkDotNetDiffer.GetMeanDelta(result.Conclusion, result.BaseResult, result.DiffResult)),
            out details);
}
