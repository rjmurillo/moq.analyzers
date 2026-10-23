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

    /// <summary>
    /// Exit code for a detected regression when <c>--failOnRegression</c> is set. It differs from
    /// <see cref="UnhandledExceptionExitCode"/> so a caller can tell "the gate tripped" from "the comparison
    /// could not run". build/scripts/perf/PerfBaselineManager.psm1 holds the same value as <c>$script:PerfRegressionExitCode</c>.
    /// </summary>
    internal const int RegressionExitCode = 3;

    internal static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = DiffCommand.CreateCommandLineOptions();
        rootCommand.SetAction(static async (parseResult, cancellationToken) =>
        {
            PerfDiffRequest request = new(
                parseResult.GetValue(DiffCommand.BaselineOption)!,
                parseResult.GetValue(DiffCommand.ResultsOption)!,
                parseResult.GetValue(DiffCommand.FailOnRegressionOption),
                parseResult.GetValue(DiffCommand.RegressedOutputOption),
                parseResult.GetValue(DiffCommand.ConfirmOnlyOption));
            string? verbosity = parseResult.GetValue(DiffCommand.VerbosityOption);

            return await RunAsync(request, verbosity, cancellationToken).ConfigureAwait(false);
        });

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

    internal static async Task<int> RunAsync(PerfDiffRequest request, string? verbosity, CancellationToken cancellationToken)
        => await RunAsync(request, verbosity, cancellationToken, PerfDiff.CompareAsync).ConfigureAwait(false);

    internal static async Task<int> RunAsync(
        PerfDiffRequest request,
        string? verbosity,
        CancellationToken cancellationToken,
        Func<PerfDiffRequest, ILogger, CancellationToken, Task<int>> compareAsync)
    {
        // Setup logging.
        LogLevel logLevel = DiffCommand.GetLogLevel(verbosity);
        ServiceProvider serviceProvider = SetupLogging(minimalLogLevel: logLevel, minimalErrorLevel: LogLevel.Warning);

        try
        {
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                return await compareAsync(request, logger, cancellationToken).ConfigureAwait(false);
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
