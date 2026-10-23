using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PerfDiff;

/// <summary>
/// Reads and writes the file that carries flagged benchmark ids between the first comparison and the
/// confirmation rerun. The format is a JSON array of strings.
/// </summary>
internal static class RegressedIdsFile
{
    /// <summary>
    /// Writes <paramref name="ids"/> as a JSON array, sorted ordinally and without duplicates.
    /// Parent folders are created when missing. An empty set writes <c>[]</c>.
    /// </summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="ids">Benchmark ids to write.</param>
    internal static void Write(string path, IEnumerable<string> ids)
    {
        string[] normalized = ids.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(normalized));
    }

    /// <summary>
    /// Reads benchmark ids written by <see cref="Write"/>.
    /// </summary>
    /// <param name="path">Source file path.</param>
    /// <param name="logger">Logger for the reason a read fails.</param>
    /// <param name="ids">The ids read, or <see langword="null"/> when the file is missing, unreadable, or malformed.</param>
    /// <returns>
    /// <see langword="true"/> when the file holds a JSON array of non-empty strings, including the empty array;
    /// otherwise <see langword="false"/>.
    /// </returns>
    internal static bool TryRead(string path, ILogger logger, out IReadOnlySet<string>? ids)
    {
        ids = null;

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
            logger.LogError("Cannot read the confirmation file '{Path}': {Reason}", path, ex.Message);
#pragma warning restore CA1848
            return false;
        }

        string[]? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<string[]>(json);
        }
        catch (JsonException ex)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
            logger.LogError("The confirmation file '{Path}' is not a JSON array of strings: {Reason}", path, ex.Message);
#pragma warning restore CA1848
            return false;
        }

        if (parsed is null || parsed.Any(string.IsNullOrEmpty))
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
            logger.LogError("The confirmation file '{Path}' must be a JSON array of non-empty strings.", path);
#pragma warning restore CA1848
            return false;
        }

        ids = new HashSet<string>(parsed, StringComparer.Ordinal);
        return true;
    }
}
