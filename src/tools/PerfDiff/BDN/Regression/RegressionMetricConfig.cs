using PerfDiff.BDN.DataContracts;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Configuration for a regression detection metric, bundling the threshold,
/// metric selectors, and display name used by <see cref="RegressionStrategyHelper"/>.
/// </summary>
public record RegressionMetricConfig(
    Threshold DisplayThreshold,
    double ThresholdValueNs,
    Func<Benchmark, double?> MetricSelector,
    Func<RegressionResult, double> DisplayValueSelector,
    string MetricName);
