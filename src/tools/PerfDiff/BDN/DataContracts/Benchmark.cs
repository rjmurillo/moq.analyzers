namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents a single benchmark, including statistics, memory usage, and measurements.
/// </summary>
public class Benchmark
{
    /// <summary>
    /// Gets or sets display information for the benchmark.
    /// </summary>
    public string? DisplayInfo { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the benchmarked method or type.
    /// </summary>
    public object? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the type name of the benchmarked method.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the method name being benchmarked.
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets the method title for display purposes.
    /// </summary>
    public string? MethodTitle { get; set; }

    /// <summary>
    /// Gets or sets the parameters used for the benchmarked method.
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the full name of the benchmarked method.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the statistics for the benchmark results.
    /// </summary>
    public Statistics? Statistics { get; set; }

    /// <summary>
    /// Gets or sets the memory usage statistics for the benchmark.
    /// </summary>
    public Memory? Memory { get; set; }

    /// <summary>
    /// Gets or sets the collection of measurements for the benchmark.
    /// </summary>
    public IEnumerable<Measurement>? Measurements { get; set; }

    /// <summary>
    /// Returns an array of the actual workload results (not warmup, not pilot).
    /// </summary>
    /// <summary>
            /// Returns an array of normalized workload times for the benchmark, including only measurements with an iteration stage of "Result".
            /// </summary>
            /// <returns>An array of measured values for the benchmark workload, calculated as nanoseconds per operation for each result measurement.</returns>
    internal double[] GetOriginalValues()
        => (Measurements ?? [])
            .Where(measurement => string.Equals(measurement.IterationStage, "Result", StringComparison.Ordinal))
            .Select(measurement => measurement.Nanoseconds / measurement.Operations)
            .ToArray();
}
