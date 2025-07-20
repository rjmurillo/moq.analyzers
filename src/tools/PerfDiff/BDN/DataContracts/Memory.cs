namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents memory usage statistics for a benchmark run.
/// </summary>
public class Memory
{
    /// <summary>
    /// Gets or sets the number of Gen0 garbage collections.
    /// </summary>
    public int Gen0Collections { get; set; }

    /// <summary>
    /// Gets or sets the number of Gen1 garbage collections.
    /// </summary>
    public int Gen1Collections { get; set; }

    /// <summary>
    /// Gets or sets the number of Gen2 garbage collections.
    /// </summary>
    public int Gen2Collections { get; set; }

    /// <summary>
    /// Gets or sets the total number of operations performed.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes allocated per operation.
    /// </summary>
    public long BytesAllocatedPerOperation { get; set; }
}
