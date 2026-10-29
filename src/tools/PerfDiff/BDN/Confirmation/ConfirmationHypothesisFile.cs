using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// Reads and writes the flagged-benchmark file that links the screen to the confirmation rounds.
/// </summary>
internal static class ConfirmationHypothesisFile
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Converters = { new StringEnumConverter() },
        Formatting = Formatting.Indented,
        MissingMemberHandling = MissingMemberHandling.Error,
    };

    /// <summary>
    /// Writes the hypotheses as a JSON array.
    /// </summary>
    /// <param name="path">The file to write.</param>
    /// <param name="hypotheses">The flagged benchmarks.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>A task that completes when the file is written.</returns>
    internal static Task WriteAsync(string path, IReadOnlyList<ConfirmationHypothesis> hypotheses, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(path, JsonConvert.SerializeObject(hypotheses, Settings), cancellationToken);

    /// <summary>
    /// Reads a JSON array of hypotheses.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>The hypotheses. An empty file array gives an empty list.</returns>
    /// <exception cref="JsonException">The file is not a valid hypothesis array.</exception>
    internal static async Task<IReadOnlyList<ConfirmationHypothesis>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonConvert.DeserializeObject<ConfirmationHypothesis[]>(json, Settings)
            ?? throw new JsonSerializationException($"Flagged benchmark file '{path}' is empty.");
    }
}
