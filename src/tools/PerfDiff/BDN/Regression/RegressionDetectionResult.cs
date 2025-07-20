using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Represents the result of a regression detection operation.
/// </summary>
public sealed class RegressionDetectionResult(Threshold threshold)
{
    /// <summary>
    /// Gets the threshold used for regression detection, if applicable.
    /// </summary>
    public Threshold Threshold { get; init; } = threshold;
}
