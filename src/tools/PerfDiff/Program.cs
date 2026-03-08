// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.CommandLine;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PerfDiff.Logging;

namespace PerfDiff;

internal sealed class Program
{
    internal const int UnhandledExceptionExitCode = 1;

    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = DiffCommand.CreateCommandLineOptions();
        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            string baseline = parseResult.GetValue(DiffCommand.BaselineOption) ?? string.Empty;
            string results = parseResult.GetValue(DiffCommand.ResultsOption) ?? string.Empty;
            string? verbosity = parseResult.GetValue(DiffCommand.VerbosityOption);
            bool failOnRegression = parseResult.GetValue(DiffCommand.FailOnRegressionOption);

            int exitCode = await RunAsync(baseline, results, verbosity, failOnRegression, cancellationToken).ConfigureAwait(false);
            parseResult.Action!.ExitCode = exitCode;
        });

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

    public static async Task<int> RunAsync(
        string baseline,
        string results,
        string? verbosity,
        bool failOnRegression,
        CancellationToken cancellationToken)
    {
        // Setup logging.
        LogLevel logLevel = GetLogLevel(verbosity);
        ServiceProvider serviceProvider = SetupLogging(minimalLogLevel: logLevel, minimalErrorLevel: LogLevel.Warning);
        await using ConfiguredAsyncDisposable asyncDisposal = serviceProvider.ConfigureAwait(false);
        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            return await PerfDiff.CompareAsync(baseline, results, failOnRegression, logger, cancellationToken).ConfigureAwait(false);
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

        static ServiceProvider SetupLogging(LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            LoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSimpleConsole(minimalLogLevel, minimalErrorLevel);
            serviceCollection.AddSingleton<ILoggerFactory>(loggerFactory);
            serviceCollection.AddLogging();

            return serviceCollection.BuildServiceProvider();
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
