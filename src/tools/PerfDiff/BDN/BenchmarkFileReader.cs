using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN;

/// <summary>
/// Reads benchmark results from files and deserializes them.
/// </summary>
public static class BenchmarkFileReader
{
    /// <summary>
    /// Attempts to read and deserialize benchmark results from the specified file paths.
    /// </summary>
    /// <param name="paths">Array of file paths to read.</param>
    /// <param name="logger">Logger for reporting errors.</param>
    /// <summary>
    /// Asynchronously reads and deserializes benchmark result files from the specified paths.
    /// </summary>
    /// <param name="paths">An array of file paths to benchmark result files.</param>
    /// <returns>A <see cref="BdnResults"/> containing the deserialized results and a flag indicating whether all files were successfully read.</returns>
    public static async Task<BdnResults> TryGetBdnResultAsync(string[] paths, ILogger logger)
    {
        BdnResult?[] results = await Task.WhenAll(paths.Select(path => ReadFromFileAsync(path, logger))).ConfigureAwait(false);
        return new BdnResults(!results.Any(x => x is null), results);
    }

    /// <summary>
    /// Asynchronously reads and deserializes a benchmark result from the specified file.
    /// </summary>
    /// <param name="resultFilePath">The path to the benchmark result file.</param>
    /// <returns>The deserialized <see cref="BdnResult"/> if successful; otherwise, null if deserialization fails.</returns>
    private static async Task<BdnResult?> ReadFromFileAsync(string resultFilePath, ILogger logger)
    {
        try
        {
            return JsonConvert.DeserializeObject<BdnResult>(await File.ReadAllTextAsync(resultFilePath).ConfigureAwait(false));
        }
        catch (JsonSerializationException)
        {
            logger.LogError("Exception while reading the {ResultFilePath} file.", resultFilePath);
            return null;
        }
    }
}
