using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

internal sealed class RegressionRatioMetricConfig(
    string metricName,
    Func<RegressionResult, double> ratioSelector,
    Func<RegressionResult, double> deltaSelector)
{
    public string MetricName { get; } = metricName;

    public Func<RegressionResult, double> RatioSelector { get; } = ratioSelector;

    public Func<RegressionResult, double> DeltaSelector { get; } = deltaSelector;
}
