using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

internal sealed class RegressionRatioMetricConfig(
    string metricName,
    Func<RegressionResult, double> ratioSelector,
    Func<RegressionResult, double> deltaSelector,
    string thresholdText = RegressionStrategyHelper.AggregateRatioRegressionThresholdText,
    double aggregateThreshold = RegressionStrategyHelper.AggregateRatioRegressionThreshold)
{
    public string MetricName { get; } = metricName;

    public Func<RegressionResult, double> RatioSelector { get; } = ratioSelector;

    public Func<RegressionResult, double> DeltaSelector { get; } = deltaSelector;

    public string ThresholdText { get; } = thresholdText;

    public double AggregateThreshold { get; } = aggregateThreshold;
}
