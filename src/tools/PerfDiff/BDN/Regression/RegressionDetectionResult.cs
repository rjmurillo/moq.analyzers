using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Represents the result of a regression detection operation.
/// </summary>
public sealed class RegressionDetectionResult(string metricName, Threshold threshold)
{
    public string MetricName { get; } = metricName;

    /// <summary>
    /// Gets the threshold used for regression detection, if applicable.
    /// </summary>
    public Threshold Threshold { get; } = threshold;
}
