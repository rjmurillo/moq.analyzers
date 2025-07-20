using Perfolizer.Mathematics.Thresholds;
using System.Collections.Generic;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Represents the result of a regression detection operation.
/// </summary>
public sealed class RegressionDetectionResult
{
    /// <summary>
    /// Gets the threshold used for regression detection, if applicable.
    /// </summary>
    public Threshold? Threshold { get; init; }

    /// <summary>
    /// Gets the list of violation messages, if any.
    /// </summary>
    public IReadOnlyList<string>? Violations { get; init; }
}
