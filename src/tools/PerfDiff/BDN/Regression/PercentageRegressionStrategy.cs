using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects median-ratio regressions only when the stable worse-set aggregate exceeds the 35% gate.
/// </summary>
public sealed class PercentageRegressionStrategy : IBenchmarkRegressionStrategy
{
    internal const double MedianAggregateRatioRegressionThreshold = 1.35D;

    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details)
        => RegressionStrategyHelper.HasAggregateRatioRegression(
            comparison,
            logger,
            new RegressionRatioMetricConfig(
                "Median ratio",
                BenchmarkDotNetDiffer.GetMedianRatio,
                GetMedianDelta,
                "35%",
                MedianAggregateRatioRegressionThreshold),
            out details);

    private static double GetMedianDelta(RegressionResult result)
    {
        Debug.Assert(result.BaseResult.Statistics != null, "Stable median results have baseline statistics.");
        Debug.Assert(result.DiffResult.Statistics != null, "Stable median results have diff statistics.");
        Statistics baseStatistics = result.BaseResult.Statistics!;
        Statistics diffStatistics = result.DiffResult.Statistics!;

        return result.Conclusion == ComparisonResult.Greater
            ? baseStatistics.Median - diffStatistics.Median
            : diffStatistics.Median - baseStatistics.Median;
    }
}
