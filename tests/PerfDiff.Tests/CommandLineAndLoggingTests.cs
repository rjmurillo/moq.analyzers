using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using PerfDiff.Logging;
using Xunit;

namespace PerfDiff.Tests;

public sealed class CommandLineAndLoggingTests
{
    [Fact]
    public void GetLogLevel_MapsAliasesAndDefaults()
    {
        (string? Verbosity, LogLevel Expected)[] cases =
        [
            ("q", LogLevel.Error),
            ("quiet", LogLevel.Error),
            ("m", LogLevel.Warning),
            ("minimal", LogLevel.Warning),
            ("n", LogLevel.Information),
            ("normal", LogLevel.Information),
            ("d", LogLevel.Debug),
            ("detailed", LogLevel.Debug),
            ("diag", LogLevel.Trace),
            ("diagnostic", LogLevel.Trace),
            ("unknown", LogLevel.Information),
            (null, LogLevel.Information),
        ];

        foreach ((string? verbosity, LogLevel expected) in cases)
        {
            LogLevel actual = DiffCommand.GetLogLevel(verbosity);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CreateCommandLineOptions_RegistersRequiredOptionsAndVerbosityValidation()
    {
        RootCommand command = DiffCommand.CreateCommandLineOptions();

        Assert.Contains(DiffCommand.BaselineOption, command.Options);
        Assert.Contains(DiffCommand.ResultsOption, command.Options);
        Assert.Contains(DiffCommand.VerbosityOption, command.Options);
        Assert.Contains(DiffCommand.FailOnRegressionOption, command.Options);
        Assert.Empty(command.Parse(["--baseline", "before", "--results", "after", "--verbosity", "diag"]).Errors);
        Assert.NotEmpty(command.Parse(["--baseline", "before", "--results", "after", "--verbosity", "verbose"]).Errors);
        Assert.NotEmpty(command.Parse(["--baseline", "before"]).Errors);
    }

    [Fact]
    public async Task ProgramRunAsync_PropagatesArgumentsToCompareDelegate()
    {
        using CancellationTokenSource source = new();
        (string Baseline, string Results, bool FailOnRegression, CancellationToken Token) observed = default;

        int exitCode = await Program.RunAsync(
            "before",
            "after",
            "quiet",
            failOnRegression: true,
            source.Token,
            (baseline, results, failOnRegression, _, token) =>
            {
                observed = (baseline, results, failOnRegression, token);
                return Task.FromResult(123);
            });

        Assert.Equal(123, exitCode);
        Assert.Equal("before", observed.Baseline);
        Assert.Equal("after", observed.Results);
        Assert.True(observed.FailOnRegression);
        Assert.Equal(source.Token, observed.Token);
    }

    [Fact]
    public async Task ProgramRunAsync_WhenCompareThrowsFileNotFound_ReturnsUnhandledExitCode()
    {
        int exitCode = await Program.RunAsync(
            "before",
            "after",
            "normal",
            failOnRegression: false,
            CancellationToken.None,
            static (_, _, _, _, _) => throw new FileNotFoundException("missing", "missing.full-compressed.json"));

        Assert.Equal(Program.UnhandledExceptionExitCode, exitCode);
    }

    [Fact]
    public async Task ProgramRunAsync_WhenCompareIsCancelled_ReturnsCancelledExitCode()
    {
        int exitCode = await Program.RunAsync(
            "before",
            "after",
            "normal",
            failOnRegression: false,
            CancellationToken.None,
            static (_, _, _, _, _) => throw new OperationCanceledException());

        Assert.Equal(Program.CancelledExitCode, exitCode);
    }

    [Fact]
    public async Task ProgramMain_WithValidArguments_InvokesFullCommandLinePath()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));

        MethodInfo main = typeof(Program).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)!;
        Task<int> exitTask = (Task<int>)main.Invoke(null, [new[] { "--baseline", baseline.Path, "--results", results.Path, "--verbosity", "quiet" }])!;
        int exitCode = await exitTask;

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void SimpleConsoleLogger_RespectsLevelAndWritesToConfiguredStreams()
    {
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        using StringWriter output = new();
        using StringWriter error = new();
        SimpleConsoleLogger logger = new(LogLevel.Information, LogLevel.Warning);

        try
        {
            Console.SetOut(output);
            Console.SetError(error);

            logger.Log(LogLevel.Debug, default, "hidden", null, static (state, _) => state);
            logger.Log(LogLevel.Information, default, "visible", null, static (state, _) => state);
            logger.Log(LogLevel.Warning, default, "warn", null, static (state, _) => state);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.DoesNotContain("hidden", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("  visible", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("warn", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SimpleConsoleLogger_BeginScopeAndProvider_ReturnNoOpObjects()
    {
        using SimpleConsoleLoggerProvider provider = new(LogLevel.Trace, LogLevel.Error);
        ILogger logger = provider.CreateLogger("PerfDiff.Tests");

        using IDisposable? scope = logger.BeginScope("scope");
        scope?.Dispose();
        provider.Dispose();

        Assert.Same(logger, provider.CreateLogger("Other"));
        Assert.Same(NullScope.Instance, scope);
    }
}
