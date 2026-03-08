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
    internal const int CancelledExitCode = 2;
    private static ParseResult? s_parseResult;

    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = DiffCommand.CreateCommandLineOptions();
        rootCommand.Handler = CommandHandler.Create(new DiffCommand.Handler(RunAsync));

        // Parse the incoming args so we can give warnings when deprecated options are used.
        s_parseResult = rootCommand.Parse(args);

        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

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
        (ILogger<Program> logger, ServiceProvider serviceProvider) = SetupLogging(console, minimalLogLevel: logLevel, minimalErrorLevel: LogLevel.Warning);

        // Hook so we can cancel and exit when ctrl+c is pressed.
        using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        try
        {
            return await PerfDiff.CompareAsync(baseline, results, failOnRegression, logger, cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (FileNotFoundException fex)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            logger.LogError(fex, "File not found: {FileName}", fex.FileName);
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogError(ILogger, string?, params object?[])'
            return UnhandledExceptionExitCode;
        }
        catch (OperationCanceledException ex)
        {
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogWarning(ILogger, string?, params object?[])'
            logger.LogWarning(ex, "Operation was cancelled.");
#pragma warning restore CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogWarning(ILogger, string?, params object?[])'
            return CancelledExitCode;
        }
        finally
        {
            await serviceProvider.DisposeAsync().ConfigureAwait(false);
        }

        static (ILogger<Program> Logger, ServiceProvider ServiceProvider) SetupLogging(IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(new LoggerFactory().AddSimpleConsole(console, minimalLogLevel, minimalErrorLevel));
            serviceCollection.AddLogging();

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            return (serviceProvider.GetRequiredService<ILogger<Program>>(), serviceProvider);
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
