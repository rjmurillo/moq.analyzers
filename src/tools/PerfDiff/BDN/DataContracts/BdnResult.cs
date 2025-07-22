namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents a set of benchmark results loaded from a file.
/// </summary>
public class BdnResult
{
    /// <summary>
    /// Gets or sets the title of the benchmark result set.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the host environment information for the benchmark run.
    /// </summary>
    public HostEnvironmentInfo? HostEnvironmentInfo { get; set; }

    /// <summary>
    /// Gets or sets the collection of benchmarks in this result set.
    /// </summary>
    public IEnumerable<Benchmark>? Benchmarks { get; set; }
}
