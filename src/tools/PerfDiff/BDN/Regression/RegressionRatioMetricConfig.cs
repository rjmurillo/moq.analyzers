using System.Diagnostics;
using PerfDiff.BDN.Confirmation;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

internal sealed class RegressionRatioMetricConfig(
    string metricName,
    ConfirmationMetric metric,
    Func<RegressionResult, double> ratioSelector,
    Func<RegressionResult, double> deltaSelector,
    string thresholdText = RegressionStrategyHelper.AggregateRatioRegressionThresholdText,
    double aggregateThreshold = RegressionStrategyHelper.AggregateRatioRegressionThreshold,
    Func<RegressionResult, double>? stabilityDeltaSelector = null,
    ConfirmationMetric? stabilityMetric = null)
{
    internal string MetricName { get; } = metricName;

    internal ConfirmationMetric Metric { get; } = metric;

    internal Func<RegressionResult, double> RatioSelector { get; } = ratioSelector;

    internal Func<RegressionResult, double> DeltaSelector { get; } = deltaSelector;

    internal Func<RegressionResult, double> StabilityDeltaSelector { get; } = GetStabilityDeltaSelector(deltaSelector, stabilityDeltaSelector);

    /// <summary>
    /// Gets the statistic that <see cref="StabilityDeltaSelector"/> reads.
    /// </summary>
    internal ConfirmationMetric StabilityMetric { get; } = stabilityMetric ?? metric;

    internal string ThresholdText { get; } = thresholdText;

    internal double AggregateThreshold { get; } = aggregateThreshold;

    private static Func<RegressionResult, double> GetStabilityDeltaSelector(
        Func<RegressionResult, double> deltaSelector,
        Func<RegressionResult, double>? stabilityDeltaSelector)
    {
        Debug.Assert(deltaSelector != null, "A delta selector is required.");
        return stabilityDeltaSelector ?? deltaSelector;
    }
}
