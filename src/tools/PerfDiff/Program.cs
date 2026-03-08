// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
            string baseline = parseResult.GetValue(DiffCommand.BaselineOption)!;
            string results = parseResult.GetValue(DiffCommand.ResultsOption)!;
            string? verbosity = parseResult.GetValue(DiffCommand.VerbosityOption);
            bool failOnRegression = parseResult.GetValue(DiffCommand.FailOnRegressionOption);

            return await RunAsync(baseline, results, verbosity, failOnRegression, cancellationToken).ConfigureAwait(false);
        });

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

    internal static async Task<int> RunAsync(
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
#pragma warning disable CA1848, CA2254 // LoggerMessage delegates, varying template
            logger.LogError(fex.Message);
#pragma warning restore CA1848, CA2254
            return UnhandledExceptionExitCode;
        }
        catch (OperationCanceledException)
        {
            return UnhandledExceptionExitCode;
        }

        static ServiceProvider SetupLogging(LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddProvider(new SimpleConsoleLoggerProvider(minimalLogLevel, minimalErrorLevel)));

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
