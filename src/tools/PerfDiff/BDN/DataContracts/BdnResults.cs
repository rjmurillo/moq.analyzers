namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents a collection of benchmark results and the success status of loading them.
/// </summary>
public class BdnResults(bool success, BdnResult?[] results)
{
    /// <summary>
    /// Gets a value indicating whether the results were loaded successfully.
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    /// Gets the array of benchmark results.
    /// </summary>
    public BdnResult?[] Results { get; } = results;

    /// <summary>
    /// Deconstructs the results into success and results array.
    /// </summary>
    /// <param name="success">Indicates whether the results were loaded successfully.</param>
    /// <summary>
    /// Deconstructs the BdnResults instance into its success status and benchmark results array.
    /// </summary>
    /// <param name="success">Outputs the success status indicating if the results were loaded successfully.</param>
    /// <param name="results">Outputs the array of benchmark results.</param>
    public void Deconstruct(out bool success, out BdnResult?[] results)
    {
        success = Success;
        results = Results;
    }
}
