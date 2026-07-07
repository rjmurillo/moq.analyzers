using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

internal sealed class RegressionRatioMetricConfig(
    string metricName,
    Func<RegressionResult, double> ratioSelector,
    Func<RegressionResult, double> deltaSelector,
    string thresholdText = RegressionStrategyHelper.AggregateRatioRegressionThresholdText,
    double aggregateThreshold = RegressionStrategyHelper.AggregateRatioRegressionThreshold)
{
    internal string MetricName { get; } = metricName;

    internal Func<RegressionResult, double> RatioSelector { get; } = ratioSelector;

    internal Func<RegressionResult, double> DeltaSelector { get; } = deltaSelector;

    internal string ThresholdText { get; } = thresholdText;

    internal double AggregateThreshold { get; } = aggregateThreshold;
}
