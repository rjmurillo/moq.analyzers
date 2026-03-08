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
    internal static Option<string> BaselineOption { get; } = CreateFilePathOption("--baseline", "folder that contains the baseline performance run data");

    /// <summary>
    /// Gets the results option.
    /// </summary>
    internal static Option<string> ResultsOption { get; } = CreateFilePathOption("--results", "folder that contains the performance results");

    /// <summary>
    /// Gets the verbosity option.
    /// </summary>
    internal static Option<string> VerbosityOption { get; } = CreateVerbosityOption();

    /// <summary>
    /// Gets the fail-on-regression option.
    /// </summary>
    internal static Option<bool> FailOnRegressionOption { get; } = new("--failOnRegression")
    {
        Description = "Should return non-zero exit code if regression detected",
    };

    private static Option<string> CreateFilePathOption(string name, string description)
    {
        Option<string> option = new(name) { Description = description, Required = true };
        option.AcceptLegalFilePathsOnly();
        return option;
    }

    private static Option<string> CreateVerbosityOption()
    {
        Option<string> option = new("--verbosity", "-v") { Description = "Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]" };
        option.AcceptOnlyFromAmong(VerbosityLevels);
        return option;
    }

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
