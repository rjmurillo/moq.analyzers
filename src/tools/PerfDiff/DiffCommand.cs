// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.CommandLine;

namespace PerfDiff;

/// <summary>
/// Provides command-line options and handler for the diff command.
/// </summary>
internal static class DiffCommand
{
    /// <summary>
    /// Delegate for handling the diff command.
    /// </summary>
    /// <param name="baseline">Baseline results folder.</param>
    /// <param name="results">Results folder.</param>
    /// <param name="verbosity">Verbosity level.</param>
    /// <param name="failOnRegression">Whether to fail on regression.</param>
    /// <param name="console">Console for output.</param>
    /// <returns>Exit code.</returns>
    internal delegate Task<int> Handler(
        string baseline,
        string results,
        string? verbosity,
        bool failOnRegression,
        IConsole console);

    /// <summary>
    /// Gets the allowed verbosity levels for the command.
    /// </summary>
    private static string[] VerbosityLevels => ["q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic"];

    /// <summary>
    /// Creates the root command with options for the diff command.
    /// </summary>
    /// <returns>The configured <see cref="RootCommand"/>.</returns>
    internal static RootCommand CreateCommandLineOptions()
    {
        // Sync changes to option and argument names with the FormatCommand.Handler above.
        RootCommand rootCommand = new RootCommand
        {
            new Option<string?>("--baseline", () => null, "folder that contains the baseline performance run data").LegalFilePathsOnly(),
            new Option<string?>("--results", () => null, "folder that contains the performance restults").LegalFilePathsOnly(),
            new Option<string>(["--verbosity", "-v"], "Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]").FromAmong(VerbosityLevels),
            new Option<bool>(["--failOnRegression"], "Should return non-zero exit code if regression detected"),
        };

        rootCommand.Description = "diff two sets of performance results";

        return rootCommand;
    }
}
