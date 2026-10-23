using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Represents the result of a regression detection operation.
/// </summary>
/// <param name="metricName">The name of the metric used for regression detection.</param>
/// <param name="threshold">The threshold used for regression detection.</param>
/// <param name="regressedIds">
/// Ids of the benchmarks that put the gate over its limit. Empty when the gate did not trip.
/// </param>
public sealed class RegressionDetectionResult(string metricName, Threshold threshold, IReadOnlyList<string>? regressedIds = null)
{
    /// <summary>
    /// Gets the name of the metric used for regression detection.
    /// </summary>
    public string MetricName { get; } = metricName;

    /// <summary>
    /// Gets the threshold used for regression detection, if applicable.
    /// </summary>
    public Threshold Threshold { get; } = threshold;

    /// <summary>
    /// Gets the ids of the benchmarks that put the gate over its limit, sorted ordinally.
    /// Empty when the gate did not trip, so a caller can union the ids of tripped gates only.
    /// </summary>
    public IReadOnlyList<string> RegressedIds { get; } = regressedIds ?? [];
}
