namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents a single measurement in a benchmark run.
/// </summary>
public class Measurement
{
    /// <summary>
    /// Gets or sets the iteration stage (e.g., Result, Warmup).
    /// </summary>
    public string? IterationStage { get; set; }

    /// <summary>
    /// Gets or sets the launch index for the measurement.
    /// </summary>
    public int LaunchIndex { get; set; }

    /// <summary>
    /// Gets or sets the iteration index for the measurement.
    /// </summary>
    public int IterationIndex { get; set; }

    /// <summary>
    /// Gets or sets the number of operations performed in this measurement.
    /// </summary>
    public long Operations { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time in nanoseconds for this measurement.
    /// </summary>
    public double Nanoseconds { get; set; }
}
