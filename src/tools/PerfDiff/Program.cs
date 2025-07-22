// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PerfDiff.Logging;

namespace PerfDiff;

internal sealed class Program
{
    internal const int UnhandledExceptionExitCode = 1;
    private static ParseResult? s_parseResult;

    /// <summary>
    /// Application entry point that parses command-line arguments, sets up the root command, and executes the appropriate handler.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>The exit code resulting from command execution.</returns>
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = DiffCommand.CreateCommandLineOptions();
        rootCommand.Handler = CommandHandler.Create(new DiffCommand.Handler(RunAsync));

        // Parse the incoming args so we can give warnings when deprecated options are used.
        s_parseResult = rootCommand.Parse(args);

        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the performance comparison between a baseline and results file, handling logging, cancellation, and error reporting.
    /// </summary>
    /// <param name="baseline">The path to the baseline results file.</param>
    /// <param name="results">The path to the results file to compare against the baseline.</param>
    /// <param name="verbosity">Optional verbosity level for logging output.</param>
    /// <param name="failOnRegression">If true, the process will return a nonzero exit code on detected regressions.</param>
    /// <returns>The exit code indicating the result of the comparison or error state.</returns>
    public static async Task<int> RunAsync(
        string baseline,
        string results,
        string? verbosity,
        bool failOnRegression,
        IConsole console)
    {
        if (s_parseResult == null)
        {
            return 1;
        }

        // Setup logging.
        LogLevel logLevel = GetLogLevel(verbosity);
        ILogger<Program> logger = SetupLogging(console, minimalLogLevel: logLevel, minimalErrorLevel: LogLevel.Warning);

        // Hook so we can cancel and exit when ctrl+c is pressed.
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        string currentDirectory = string.Empty;

        try
        {
            int exitCode = await PerfDiff.CompareAsync(baseline, results, failOnRegression, logger, cancellationTokenSource.Token).ConfigureAwait(false);
            return exitCode;
        }
        catch (FileNotFoundException fex)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning disable CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            logger.LogError(fex.Message);
#pragma warning restore CA2254 // The logging message template should not vary between calls to 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            return UnhandledExceptionExitCode;
        }
        catch (OperationCanceledException)
        {
            return UnhandledExceptionExitCode;
        }
        finally
        {
            if (!string.IsNullOrEmpty(currentDirectory))
            {
                Environment.CurrentDirectory = currentDirectory;
            }
        }

        static ILogger<Program> SetupLogging(IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new LoggerFactory().AddSimpleConsole(console, minimalLogLevel, minimalErrorLevel));
            serviceCollection.AddLogging();

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            ILogger<Program>? logger = serviceProvider.GetService<ILogger<Program>>();

            return logger!;
        }

        static LogLevel GetLogLevel(string? verbosity)
            => verbosity switch
            {
                "q" or "quiet" => LogLevel.Error,
                "m" or "minimal" => LogLevel.Warning,
                "n" or "normal" => LogLevel.Information,
                "d" or "detailed" => LogLevel.Debug,
                "diag" or "diagnostic" => LogLevel.Trace,
                _ => LogLevel.Information,
            };
    }
}
