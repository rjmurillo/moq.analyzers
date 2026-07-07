using System.Diagnostics;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

internal sealed class RegressionRatioMetricConfig(
    string metricName,
    Func<RegressionResult, double> ratioSelector,
    Func<RegressionResult, double> deltaSelector,
    string thresholdText = RegressionStrategyHelper.AggregateRatioRegressionThresholdText,
    double aggregateThreshold = RegressionStrategyHelper.AggregateRatioRegressionThreshold,
    Func<RegressionResult, double>? stabilityDeltaSelector = null)
{
    internal string MetricName { get; } = metricName;

    internal Func<RegressionResult, double> RatioSelector { get; } = ratioSelector;

    internal Func<RegressionResult, double> DeltaSelector { get; } = deltaSelector;

    internal Func<RegressionResult, double> StabilityDeltaSelector { get; } = GetStabilityDeltaSelector(deltaSelector, stabilityDeltaSelector);

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
