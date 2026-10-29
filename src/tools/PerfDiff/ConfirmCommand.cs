using System.CommandLine;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.Confirmation;

namespace PerfDiff;

/// <summary>
/// The <c>confirm</c> subcommand. It decides whether paired rounds reproduce flagged regressions.
/// </summary>
internal static class ConfirmCommand
{
    internal const int NotConfirmedExitCode = 0;
    internal const int ErrorExitCode = 1;
    internal const int ContinueExitCode = 3;
    internal const int ConfirmedExitCode = 4;

    internal const int DefaultMinRounds = 3;
    internal const int DefaultMaxRounds = 10;

    internal static Option<string> FlaggedOption { get; } = DiffCommand.CreateFilePathOption("--flagged", "file that lists the flagged benchmarks");

    internal static Option<string> RoundsOption { get; } = DiffCommand.CreateFilePathOption("--rounds", "folder that holds the baseline/rN and current/rN round results");

    internal static Option<int> MinRoundsOption { get; } = new("--minRounds")
    {
        Description = "fewest rounds before the stop rule applies",
        DefaultValueFactory = static _ => DefaultMinRounds,
    };

    internal static Option<int> MaxRoundsOption { get; } = new("--maxRounds")
    {
        Description = "round count that forces a decision",
        DefaultValueFactory = static _ => DefaultMaxRounds,
    };

    internal static Option<string> VerbosityOption { get; } = DiffCommand.CreateVerbosityOption();

    internal static Command Create()
    {
        Command command = new("confirm", "decide whether paired confirmation rounds reproduce flagged regressions")
        {
            FlaggedOption,
            RoundsOption,
            MinRoundsOption,
            MaxRoundsOption,
            VerbosityOption,
        };

        command.SetAction(static async (parseResult, cancellationToken) => await Program.RunWithLoggingAsync(
            parseResult.GetValue(VerbosityOption),
            logger => RunAsync(
                new ConfirmOptions(
                    parseResult.GetValue(FlaggedOption)!,
                    parseResult.GetValue(RoundsOption)!,
                    parseResult.GetValue(MinRoundsOption),
                    parseResult.GetValue(MaxRoundsOption)),
                logger,
                cancellationToken)).ConfigureAwait(false));

        return command;
    }

    /// <summary>
    /// Reads the flagged benchmarks and the rounds, then maps the decision to an exit code.
    /// </summary>
    /// <param name="options">The parsed command options.</param>
    /// <param name="logger">Receives the per-benchmark results.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>One of the exit code constants on this class.</returns>
    internal static async Task<int> RunAsync(ConfirmOptions options, ILogger logger, CancellationToken cancellationToken)
    {
        (string flaggedPath, string roundsRoot, int minRounds, int maxRounds) = options;
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
        if (minRounds < ConfirmationAnalyzer.MinimumRounds || maxRounds < minRounds)
        {
            logger.LogError("--minRounds must be at least 2 and --maxRounds at least --minRounds. Got {MinRounds} and {MaxRounds}.", minRounds, maxRounds);
            return ErrorExitCode;
        }

        IReadOnlyList<ConfirmationHypothesis> hypotheses;
        try
        {
            hypotheses = await ConfirmationHypothesisFile.ReadAsync(flaggedPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            logger.LogError(ex, "Failed to read flagged benchmark file {FlaggedPath}.", flaggedPath);
            return ErrorExitCode;
        }
#pragma warning restore CA1848

        IReadOnlyList<ConfirmationRound>? rounds = await ConfirmationRoundReader.TryReadRoundsAsync(roundsRoot, logger, cancellationToken).ConfigureAwait(false);
        if (rounds is null)
        {
            return ErrorExitCode;
        }

        return ToExitCode(ConfirmationAnalyzer.Analyze(hypotheses, rounds, minRounds, maxRounds, logger));
    }

    /// <summary>
    /// Maps a decision to its exit code. Any other value maps to the error code.
    /// </summary>
    /// <param name="decision">The confirmation decision.</param>
    /// <returns>One of the exit code constants on this class.</returns>
    internal static int ToExitCode(ConfirmationDecision decision)
        => decision switch
        {
            ConfirmationDecision.NotConfirmed => NotConfirmedExitCode,
            ConfirmationDecision.Continue => ContinueExitCode,
            ConfirmationDecision.Confirmed => ConfirmedExitCode,
            _ => ErrorExitCode,
        };
}
