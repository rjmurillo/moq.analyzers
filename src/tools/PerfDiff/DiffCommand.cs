// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.CommandLine;

namespace PerfDiff;

/// <summary>
/// Provides command-line options and handler for the diff command.
/// </summary>
internal static class DiffCommand
{
    /// <summary>
    /// Gets the allowed verbosity levels for the command.
    /// </summary>
    private static readonly string[] VerbosityLevels = ["q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic"];

    /// <summary>
    /// Gets the baseline option.
    /// </summary>
    internal static Option<string> BaselineOption { get; } =
        new Option<string>("--baseline", "folder that contains the baseline performance run data") { IsRequired = true }.LegalFilePathsOnly();

    /// <summary>
    /// Gets the results option.
    /// </summary>
    internal static Option<string> ResultsOption { get; } =
        new Option<string>("--results", "folder that contains the performance results") { IsRequired = true }.LegalFilePathsOnly();

    /// <summary>
    /// Gets the verbosity option.
    /// </summary>
    internal static Option<string> VerbosityOption { get; } =
        new Option<string>(["--verbosity", "-v"], "Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]").FromAmong(VerbosityLevels);

    /// <summary>
    /// Gets the fail-on-regression option.
    /// </summary>
    internal static Option<bool> FailOnRegressionOption { get; } =
        new(["--failOnRegression"], "Should return non-zero exit code if regression detected");

    /// <summary>
    /// Creates the root command with options for the diff command.
    /// </summary>
    /// <returns>The configured <see cref="RootCommand"/>.</returns>
    internal static RootCommand CreateCommandLineOptions()
    {
        RootCommand rootCommand = new RootCommand("diff two sets of performance results")
        {
            BaselineOption,
            ResultsOption,
            VerbosityOption,
            FailOnRegressionOption,
        };

        return rootCommand;
    }
}
