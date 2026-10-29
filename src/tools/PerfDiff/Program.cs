// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
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

    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = DiffCommand.CreateCommandLineOptions();
        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

    internal static async Task<int> RunAsync(
        string baseline,
        string results,
        string? verbosity,
        bool failOnRegression,
        CancellationToken cancellationToken,
        Func<string, string, bool, ILogger, CancellationToken, Task<int>> compareAsync)
        => await RunWithLoggingAsync(verbosity, logger => compareAsync(baseline, results, failOnRegression, logger, cancellationToken)).ConfigureAwait(false);

    /// <summary>
    /// Runs a command body with console logging and maps known exceptions to exit codes.
    /// </summary>
    /// <param name="verbosity">The verbosity string from the command line.</param>
    /// <param name="runAsync">The command body.</param>
    /// <returns>The body's exit code, or an exit code for a known exception.</returns>
    internal static async Task<int> RunWithLoggingAsync(string? verbosity, Func<ILogger, Task<int>> runAsync)
    {
        // Setup logging.
        LogLevel logLevel = DiffCommand.GetLogLevel(verbosity);
        ServiceProvider serviceProvider = SetupLogging(minimalLogLevel: logLevel, minimalErrorLevel: LogLevel.Warning);

        try
        {
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                return await runAsync(logger).ConfigureAwait(false);
            }
            catch (FileNotFoundException fex)
            {
#pragma warning disable CA1848, CA2254 // LoggerMessage delegates, varying template
                logger.LogError(fex, "File not found: {FileName}", fex.FileName);
#pragma warning restore CA1848, CA2254
                return UnhandledExceptionExitCode;
            }
            catch (OperationCanceledException ex)
            {
#pragma warning disable CA1848 // LoggerMessage delegates
                logger.LogWarning(ex, "Operation was cancelled.");
#pragma warning restore CA1848
                return CancelledExitCode;
            }
        }
        finally
        {
            await serviceProvider.DisposeAsync().ConfigureAwait(false);
        }

        [ExcludeFromCodeCoverage(Justification = "Microsoft.Extensions.Logging builder emits compiler branches that are not part of PerfDiff behavior.")]
        static ServiceProvider SetupLogging(LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(minimalLogLevel);
                builder.AddProvider(new SimpleConsoleLoggerProvider(minimalLogLevel, minimalErrorLevel));
            });

            return serviceCollection.BuildServiceProvider();
        }
    }
}
