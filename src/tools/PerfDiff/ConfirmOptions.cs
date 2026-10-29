namespace PerfDiff;

/// <summary>
/// The parsed options of the <c>confirm</c> subcommand.
/// </summary>
/// <param name="FlaggedPath">The flagged benchmark file.</param>
/// <param name="RoundsRoot">The confirmation output root.</param>
/// <param name="MinRounds">The fewest rounds before the stop rule applies.</param>
/// <param name="MaxRounds">The round count that forces a decision.</param>
internal sealed record ConfirmOptions(string FlaggedPath, string RoundsRoot, int MinRounds, int MaxRounds);
