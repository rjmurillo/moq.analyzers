// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Frozen;
using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace PerfDiff;

/// <summary>
/// Provides command-line options and handler for the diff command.
/// </summary>
internal static class DiffCommand
{
    /// <summary>
    /// Maps verbosity strings to their corresponding log levels.
    /// </summary>
    private static readonly FrozenDictionary<string, LogLevel> VerbosityMap = new Dictionary<string, LogLevel>
    {
        ["q"] = LogLevel.Error,
        ["quiet"] = LogLevel.Error,
        ["m"] = LogLevel.Warning,
        ["minimal"] = LogLevel.Warning,
        ["n"] = LogLevel.Information,
        ["normal"] = LogLevel.Information,
        ["d"] = LogLevel.Debug,
        ["detailed"] = LogLevel.Debug,
        ["diag"] = LogLevel.Trace,
        ["diagnostic"] = LogLevel.Trace,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>
    /// Returns the <see cref="LogLevel"/> for the given verbosity string.
    /// Falls back to <see cref="LogLevel.Information"/> when the value is null or unrecognized.
    /// </summary>
    /// <param name="verbosity">The verbosity string from the command line.</param>
    /// <returns>The corresponding <see cref="LogLevel"/>.</returns>
    internal static LogLevel GetLogLevel(string? verbosity)
        => verbosity is not null && VerbosityMap.TryGetValue(verbosity, out LogLevel level)
            ? level
            : LogLevel.Information;

    private static Option<string> CreateVerbosityOption()
    {
        Option<string> option = new("--verbosity", "-v") { Description = "Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]" };
        option.AcceptOnlyFromAmong(VerbosityMap.Keys.ToArray());
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
