using System.CommandLine;
using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using Xunit;

namespace PerfDiff.Tests;

/// <summary>
/// Covers --regressedOutput and --confirmOnly: the first round names the flagged benchmarks, the rerun
/// fails only on benchmarks flagged in both rounds.
/// </summary>
public sealed class ConfirmationRerunTests
{
    private static readonly double OverBudgetNs = 300_000_000D;
    private static readonly double UnderBudgetNs = 10_000_000D;

    [Fact]
    public async Task CompareAsync_WithRegressedOutput_WritesFlaggedIdsAndReturnsRegressionExitCode()
    {
        using RegressedIdsTests.TempFolder folder = new();
        string output = Path.Combine(folder.Path, "round1", "ids.json");
        using BenchmarkTestData.ResultDirectory baseline = Results(("Zeta", UnderBudgetNs), ("Alpha", UnderBudgetNs), ("Quiet", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Zeta", OverBudgetNs), ("Alpha", OverBudgetNs), ("Quiet", UnderBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, RegressedOutputPath: output), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(Program.RegressionExitCode, exitCode);
        Assert.Equal("""["Alpha","Zeta"]""", await File.ReadAllTextAsync(output).ConfigureAwait(true));
    }

    [Fact]
    public async Task CompareAsync_WithRegressedOutputAndNoRegression_WritesEmptyArray()
    {
        using RegressedIdsTests.TempFolder folder = new();
        string output = Path.Combine(folder.Path, "ids.json");
        await File.WriteAllTextAsync(output, """["stale"]""").ConfigureAwait(true);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", UnderBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, RegressedOutputPath: output), new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(0, exitCode);
        Assert.Equal("[]", await File.ReadAllTextAsync(output).ConfigureAwait(true));
    }

    [Fact]
    public async Task CompareAsync_WithRegressedOutputAndFailOnRegressionFalse_WritesIdsAndReturnsZero()
    {
        using RegressedIdsTests.TempFolder folder = new();
        string output = Path.Combine(folder.Path, "ids.json");
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: false, RegressedOutputPath: output), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("""["Alpha"]""", await File.ReadAllTextAsync(output).ConfigureAwait(true));
    }

    [Fact]
    public async Task CompareAsync_WhenComparisonFails_DoesNotWriteRegressedOutput()
    {
        using RegressedIdsTests.TempFolder folder = new();
        string output = Path.Combine(folder.Path, "ids.json");
        using BenchmarkTestData.ResultDirectory baseline = new();
        using BenchmarkTestData.ResultDirectory results = new();

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, RegressedOutputPath: output), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(Program.UnhandledExceptionExitCode, exitCode);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public async Task CompareAsync_WhenRegressedOutputCannotBeWritten_ReturnsErrorExitCode()
    {
        using RegressedIdsTests.TempFolder folder = new();
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs));
        PerfDiffTestLogger logger = new();

        // The destination is an existing folder, so the write fails.
        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, RegressedOutputPath: folder.Path), logger, CancellationToken.None);

        Assert.Equal(Program.UnhandledExceptionExitCode, exitCode);
        Assert.Contains(logger.Messages, message => message.Contains("Cannot write the regressed benchmark ids", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompareAsync_WhenFlaggedInBothRounds_ConfirmsRegression()
    {
        using ConfirmFixture fixture = new(roundOneIds: ["Alpha"]);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs), ("Beta", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs), ("Beta", OverBudgetNs));
        string output = Path.Combine(fixture.Folder, "round2.json");

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, RegressedOutputPath: output, ConfirmOnlyPath: fixture.ConfirmFile), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(Program.RegressionExitCode, exitCode);
        Assert.Equal("""["Alpha"]""", await File.ReadAllTextAsync(output).ConfigureAwait(true));
    }

    [Fact]
    public async Task CompareAsync_WhenRoundOneFlaggedBenchmarkIsQuietInRoundTwo_DoesNotConfirm()
    {
        using ConfirmFixture fixture = new(roundOneIds: ["Alpha"]);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", UnderBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, ConfirmOnlyPath: fixture.ConfirmFile), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task CompareAsync_WhenRoundTwoFlagsOnlyBenchmarksOutsideRoundOne_DoesNotConfirm()
    {
        using ConfirmFixture fixture = new(roundOneIds: ["Alpha"]);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs), ("Beta", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", UnderBudgetNs), ("Beta", OverBudgetNs));
        string output = Path.Combine(fixture.Folder, "round2.json");

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, RegressedOutputPath: output, ConfirmOnlyPath: fixture.ConfirmFile), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("[]", await File.ReadAllTextAsync(output).ConfigureAwait(true));
    }

    [Fact]
    public async Task CompareAsync_WhenConfirmFileIsEmptyArray_DoesNotConfirmAnything()
    {
        using ConfirmFixture fixture = new(roundOneIds: []);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, ConfirmOnlyPath: fixture.ConfirmFile), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task CompareAsync_WhenConfirmedAndFailOnRegressionIsFalse_ReturnsZero()
    {
        using ConfirmFixture fixture = new(roundOneIds: ["Alpha"]);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: false, ConfirmOnlyPath: fixture.ConfirmFile), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task CompareAsync_WhenConfirmFileIsMissing_ReturnsErrorNotPass()
    {
        using RegressedIdsTests.TempFolder folder = new();
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", UnderBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, ConfirmOnlyPath: Path.Combine(folder.Path, "missing.json")), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(Program.UnhandledExceptionExitCode, exitCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"ids":["Alpha"]}""")]
    public async Task CompareAsync_WhenConfirmFileIsMalformed_ReturnsErrorNotPass(string content)
    {
        using ConfirmFixture fixture = new(rawContent: content);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs));

        int exitCode = await PerfDiff.CompareAsync(new PerfDiffRequest(baseline.Path, results.Path, FailOnRegression: true, ConfirmOnlyPath: fixture.ConfirmFile), new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(Program.UnhandledExceptionExitCode, exitCode);
    }

    [Fact]
    public async Task ComparisonService_WithConfirmOnlyIds_GatesOnlyListedBenchmarks()
    {
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs), ("Beta", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs), ("Beta", OverBudgetNs));
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None, new HashSet<string>(StringComparer.Ordinal) { "Beta", "NotInThisRun" });

        Assert.True(comparison.RegressionDetected);
        Assert.Equal(["Beta"], comparison.RegressedIds);
        Assert.Contains(logger.Messages, message => message.Contains("gating 1 of 2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ComparisonService_WithNullConfirmOnlyIds_GatesEverything()
    {
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs), ("Beta", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs), ("Beta", OverBudgetNs));
        BenchmarkComparisonService service = new(new PerfDiffTestLogger());

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None, confirmOnlyIds: null);

        Assert.Equal(["Alpha", "Beta"], comparison.RegressedIds);
    }

    [Fact]
    public void CreateCommandLineOptions_RegistersConfirmationOptionsAsOptional()
    {
        RootCommand command = DiffCommand.CreateCommandLineOptions();

        Assert.Contains(DiffCommand.RegressedOutputOption, command.Options);
        Assert.Contains(DiffCommand.ConfirmOnlyOption, command.Options);
        Assert.Empty(command.Parse(["--baseline", "before", "--results", "after"]).Errors);
        Assert.Empty(command.Parse(["--baseline", "before", "--results", "after", "--regressedOutput", "ids.json", "--confirmOnly", "round1.json"]).Errors);
    }

    [Fact]
    public async Task ProgramMain_WithConfirmationOptions_WritesIdsAndReturnsRegressionExitCode()
    {
        using ConfirmFixture fixture = new(roundOneIds: ["Alpha"]);
        using BenchmarkTestData.ResultDirectory baseline = Results(("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = Results(("Alpha", OverBudgetNs));
        string output = Path.Combine(fixture.Folder, "round2.json");

        string[] args = ["--baseline", baseline.Path, "--results", results.Path, "--verbosity", "quiet", "--failOnRegression", "--regressedOutput", output, "--confirmOnly", fixture.ConfirmFile];
        int exitCode = await Program.Main(args);

        Assert.Equal(Program.RegressionExitCode, exitCode);
        Assert.Equal("""["Alpha"]""", await File.ReadAllTextAsync(output).ConfigureAwait(true));
    }

    [Fact]
    public void RegressionExitCode_DiffersFromOtherExitCodes()
    {
        const int expectedRegressionExitCode = 3;
        Assert.Equal(expectedRegressionExitCode, Program.RegressionExitCode);
        Assert.NotEqual(Program.UnhandledExceptionExitCode, Program.RegressionExitCode);
        Assert.NotEqual(Program.CancelledExitCode, Program.RegressionExitCode);
    }

    private static BenchmarkTestData.ResultDirectory Results(params (string Id, double Ns)[] benchmarks)
        => BenchmarkTestData.CreateResultDirectory(benchmarks.Select(benchmark => BenchmarkTestData.CreateBenchmark(
            benchmark.Id,
            medianNs: benchmark.Ns,
            meanNs: benchmark.Ns,
            p95Ns: benchmark.Ns,
            measurements: BenchmarkTestData.CreateMeasurements(benchmark.Ns, benchmark.Ns, benchmark.Ns, benchmark.Ns, benchmark.Ns))).ToArray());

    private sealed class ConfirmFixture : IDisposable
    {
        private readonly RegressedIdsTests.TempFolder _folder = new();

        public ConfirmFixture(string[]? roundOneIds = null, string? rawContent = null)
        {
            ConfirmFile = Path.Combine(_folder.Path, "round1.json");
            if (rawContent is not null)
            {
                File.WriteAllText(ConfirmFile, rawContent);
            }
            else
            {
                RegressedIdsFile.Write(ConfirmFile, roundOneIds ?? []);
            }
        }

        public string Folder => _folder.Path;

        public string ConfirmFile { get; }

        public void Dispose() => _folder.Dispose();
    }
}
