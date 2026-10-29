using System.CommandLine;
using System.Text.Json;
using PerfDiff.BDN;
using PerfDiff.BDN.Confirmation;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Xunit;

namespace PerfDiff.Tests;

public sealed class ConfirmationIOTests
{
    private static readonly string BenchmarkId = "Ns.Bench.Method(FileCount: 1000)";

    [Fact]
    public async Task TryReadRoundsAsync_WithPairedRounds_ReturnsRoundsInOrder()
    {
        using RoundsRoot root = new();
        root.WriteRound("baseline", 1, 100);
        root.WriteRound("current", 1, 110);
        root.WriteRound("baseline", 2, 200);
        root.WriteRound("current", 2, 220);
        Directory.CreateDirectory(Path.Combine(root.Path, "baseline", "notes"));
        Directory.CreateDirectory(Path.Combine(root.Path, "current", "rx"));

        IReadOnlyList<ConfirmationRound>? rounds = await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);

        Assert.NotNull(rounds);
        Assert.Equal(2, rounds.Count);
        Assert.Equal(100, rounds[0].Baseline[BenchmarkId].Statistics!.Mean);
        Assert.Equal(220, rounds[1].Current[BenchmarkId].Statistics!.Mean);
    }

    [Fact]
    public async Task TryReadRoundsAsync_WithoutCurrentFolder_ReturnsNull()
    {
        using RoundsRoot root = new();
        root.WriteRound("baseline", 1, 100);
        PerfDiffTestLogger logger = new();

        IReadOnlyList<ConfirmationRound>? rounds = await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, logger, CancellationToken.None).ConfigureAwait(true);

        Assert.Null(rounds);
        Assert.Contains(logger.Messages, message => message.Contains("needs 'baseline' and 'current'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryReadRoundsAsync_WithoutBaselineFolder_ReturnsNull()
    {
        using RoundsRoot root = new();
        root.WriteRound("current", 1, 100);

        Assert.Null(await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true));
    }

    [Fact]
    public async Task TryReadRoundsAsync_WithUnpairedRound_ReturnsNull()
    {
        using RoundsRoot root = new();
        root.WriteRound("baseline", 1, 100);
        root.WriteRound("current", 1, 100);
        root.WriteRound("baseline", 2, 100);
        PerfDiffTestLogger logger = new();

        IReadOnlyList<ConfirmationRound>? rounds = await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, logger, CancellationToken.None).ConfigureAwait(true);

        Assert.Null(rounds);
        Assert.Contains(logger.Messages, message => message.Contains("Baseline: [1, 2]. Current: [1]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryReadRoundsAsync_WithGapInRounds_ReturnsNull()
    {
        using RoundsRoot root = new();
        root.WriteRound("baseline", 1, 100);
        root.WriteRound("current", 1, 100);
        root.WriteRound("baseline", 3, 100);
        root.WriteRound("current", 3, 100);

        Assert.Null(await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true));
    }

    [Fact]
    public async Task TryReadRoundsAsync_WithRoundWithoutReport_ReturnsNull()
    {
        using RoundsRoot root = new();
        root.WriteRound("baseline", 1, 100);
        Directory.CreateDirectory(Path.Combine(root.Path, "current", "r1"));
        PerfDiffTestLogger logger = new();

        IReadOnlyList<ConfirmationRound>? rounds = await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, logger, CancellationToken.None).ConfigureAwait(true);

        Assert.Null(rounds);
        Assert.Contains(logger.Messages, message => message.Contains("has no '*full-compressed.json' report", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryReadRoundsAsync_WithUnreadableReport_ReturnsNull()
    {
        using RoundsRoot root = new();
        root.WriteRound("baseline", 1, 100);
        string current = Directory.CreateDirectory(Path.Combine(root.Path, "current", "r1")).FullName;
        await File.WriteAllTextAsync(Path.Combine(current, "x.full-compressed.json"), "{ not json").ConfigureAwait(true);

        Assert.Null(await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true));
    }

    [Fact]
    public async Task TryReadRoundsAsync_WithInvalidBenchmark_ReturnsNull()
    {
        using RoundsRoot root = new();
        root.WriteRound("baseline", 1, 100);
        root.WriteRound("current", 1, 100, BenchmarkTestData.CreateMeasurements(100));

        Assert.Null(await ConfirmationRoundReader.TryReadRoundsAsync(root.Path, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true));
    }

    [Fact]
    public async Task HypothesisFile_RoundTripsWithReadableMetric()
    {
        using RoundsRoot root = new();
        string path = Path.Combine(root.Path, "flagged.json");
        ConfirmationHypothesis[] hypotheses =
        [
            new(BenchmarkId, ConfirmationMetric.P95, 0D, 250_000_000D),
            new("Ns.Bench.Other", ConfirmationMetric.Median, 0.35, BudgetNs: null),
        ];

        await ConfirmationHypothesisFile.WriteAsync(path, hypotheses, CancellationToken.None).ConfigureAwait(true);
        IReadOnlyList<ConfirmationHypothesis> read = await ConfirmationHypothesisFile.ReadAsync(path, CancellationToken.None).ConfigureAwait(true);
        string json = await File.ReadAllTextAsync(path).ConfigureAwait(true);

        Assert.Equal(hypotheses, read);
        Assert.Contains("\"Metric\": \"P95\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HypothesisFile_WhenJsonIsNull_Throws()
    {
        using RoundsRoot root = new();
        string path = Path.Combine(root.Path, "flagged.json");
        await File.WriteAllTextAsync(path, "null").ConfigureAwait(true);

        await Assert.ThrowsAsync<Newtonsoft.Json.JsonSerializationException>(() => ConfirmationHypothesisFile.ReadAsync(path, CancellationToken.None)).ConfigureAwait(true);
    }

    [Fact]
    public async Task ConfirmRunAsync_MapsEachDecisionToItsExitCode()
    {
        Assert.Equal(ConfirmCommand.ConfirmedExitCode, await RunConfirmAsync(1.10, 1.10, 1.10).ConfigureAwait(true));
        Assert.Equal(ConfirmCommand.NotConfirmedExitCode, await RunConfirmAsync(1.0, 1.0, 1.0).ConfigureAwait(true));
        Assert.Equal(ConfirmCommand.ContinueExitCode, await RunConfirmAsync(1.10, 1.10).ConfigureAwait(true));
    }

    [Fact]
    public void ToExitCode_MapsEveryDecision()
    {
        Assert.Equal(ConfirmCommand.NotConfirmedExitCode, ConfirmCommand.ToExitCode(ConfirmationDecision.NotConfirmed));
        Assert.Equal(ConfirmCommand.ContinueExitCode, ConfirmCommand.ToExitCode(ConfirmationDecision.Continue));
        Assert.Equal(ConfirmCommand.ConfirmedExitCode, ConfirmCommand.ToExitCode(ConfirmationDecision.Confirmed));
        Assert.Equal(ConfirmCommand.ErrorExitCode, ConfirmCommand.ToExitCode(ConfirmationDecision.Error));
        Assert.Equal(ConfirmCommand.ErrorExitCode, ConfirmCommand.ToExitCode((ConfirmationDecision)99));
    }

    [Fact]
    public async Task ConfirmRunAsync_WhenRoundsAreUnreadable_ReturnsError()
    {
        using RoundsRoot root = new();
        string flagged = await root.WriteFlaggedAsync().ConfigureAwait(true);

        int exitCode = await ConfirmCommand.RunAsync(new ConfirmOptions(flagged, root.Path, 3, 10), new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(ConfirmCommand.ErrorExitCode, exitCode);
    }

    [Fact]
    public async Task ConfirmRunAsync_WhenAnalysisFails_ReturnsError()
    {
        using RoundsRoot root = new();
        string flagged = await root.WriteFlaggedAsync("Ns.Missing").ConfigureAwait(true);
        root.WriteRound("baseline", 1, 100);
        root.WriteRound("current", 1, 100);

        int exitCode = await ConfirmCommand.RunAsync(new ConfirmOptions(flagged, root.Path, 3, 10), new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(ConfirmCommand.ErrorExitCode, exitCode);
    }

    [Fact]
    public async Task ConfirmRunAsync_WhenFlaggedFileIsInvalid_ReturnsError()
    {
        using RoundsRoot root = new();
        string flagged = Path.Combine(root.Path, "flagged.json");
        await File.WriteAllTextAsync(flagged, "{ not json").ConfigureAwait(true);
        PerfDiffTestLogger logger = new();

        int exitCode = await ConfirmCommand.RunAsync(new ConfirmOptions(flagged, root.Path, 3, 10), logger, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(ConfirmCommand.ErrorExitCode, exitCode);
        Assert.Contains(logger.Messages, message => message.Contains("Failed to read flagged benchmark file", StringComparison.Ordinal));
    }

    [Fact]
    public void ConfirmCommand_IsRegisteredWithRequiredOptions()
    {
        RootCommand command = DiffCommand.CreateCommandLineOptions();

        Assert.Contains(command.Subcommands, subcommand => string.Equals(subcommand.Name, "confirm", StringComparison.Ordinal));
        Assert.Empty(command.Parse(["confirm", "--flagged", "f.json", "--rounds", "r"]).Errors);
        Assert.Equal(ConfirmCommand.DefaultMinRounds, command.Parse(["confirm", "--flagged", "f.json", "--rounds", "r"]).GetValue(ConfirmCommand.MinRoundsOption));
        Assert.Equal(ConfirmCommand.DefaultMaxRounds, command.Parse(["confirm", "--flagged", "f.json", "--rounds", "r"]).GetValue(ConfirmCommand.MaxRoundsOption));
        Assert.NotEmpty(command.Parse(["confirm", "--rounds", "r"]).Errors);
    }

    [Fact]
    public async Task ConfirmRunAsync_WithInvalidRoundBounds_ReturnsError()
    {
        using RoundsRoot root = new();
        string flagged = await root.WriteFlaggedAsync().ConfigureAwait(true);
        PerfDiffTestLogger logger = new();

        Assert.Equal(ConfirmCommand.ErrorExitCode, await ConfirmCommand.RunAsync(new ConfirmOptions(flagged, root.Path, 1, 10), logger, CancellationToken.None).ConfigureAwait(true));
        Assert.Equal(ConfirmCommand.ErrorExitCode, await ConfirmCommand.RunAsync(new ConfirmOptions(flagged, root.Path, 4, 3), logger, CancellationToken.None).ConfigureAwait(true));
        Assert.Equal(ConfirmCommand.ErrorExitCode, await ConfirmCommand.RunAsync(new ConfirmOptions(flagged, root.Path, 2, 2), logger, CancellationToken.None).ConfigureAwait(true));
        Assert.Contains(logger.Messages, message => message.Contains("Got 1 and 10", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Got 4 and 3", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Got 2 and 2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConfirmCommand_InvokesFromCommandLine()
    {
        using RoundsRoot root = new();
        string flagged = await root.WriteFlaggedAsync().ConfigureAwait(true);
        for (int round = 1; round <= 3; round++)
        {
            root.WriteRound("baseline", round, 100);
            root.WriteRound("current", round, 100);
        }

        int exitCode = await DiffCommand.CreateCommandLineOptions()
            .Parse(["confirm", "--flagged", flagged, "--rounds", root.Path, "-v", "quiet"])
            .InvokeAsync()
            .ConfigureAwait(true);

        Assert.Equal(ConfirmCommand.NotConfirmedExitCode, exitCode);
    }

    [Fact]
    public void BudgetGate_FlagsCrossingBenchmarkWithBudget()
    {
        PercentileRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            BenchmarkId,
            BenchmarkTestData.CreateBenchmark(BenchmarkId, p95Ns: 200_000_000D),
            BenchmarkTestData.CreateBenchmark(BenchmarkId, p95Ns: 260_000_000D));

        Assert.True(strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details));

        ConfirmationHypothesis flagged = Assert.Single(details.Flagged);
        Assert.Equal(new ConfirmationHypothesis(BenchmarkId, ConfirmationMetric.P95, 0D, 250_000_000D), flagged);
    }

    [Fact]
    public void BudgetGate_WithoutCrossing_FlagsNothing()
    {
        MeanWallClockRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            BenchmarkId,
            BenchmarkTestData.CreateBenchmark(BenchmarkId, meanNs: 10D),
            BenchmarkTestData.CreateBenchmark(BenchmarkId, meanNs: 10D));

        Assert.False(strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details));
        Assert.Empty(details.Flagged);
    }

    [Fact]
    public async Task ComparisonService_FlagsRatioGateBenchmarksWithMinimumEffect()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: 10_000_000, meanNs: 10_000_000, p95Ns: 10_000_000, BenchmarkTestData.CreateMeasurements(10_000_000, 10_100_000, 9_900_000, 10_000_000, 10_050_000)));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: 20_000_000, meanNs: 20_000_000, p95Ns: 20_000_000, BenchmarkTestData.CreateMeasurements(20_000_000, 20_100_000, 19_900_000, 20_000_000, 20_050_000)));
        BenchmarkComparisonService service = new(new PerfDiffTestLogger());

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None).ConfigureAwait(true);

        Assert.True(comparison.RegressionDetected);
        Assert.Equal(3, comparison.FlaggedOrEmpty.Count);
        AssertFlagged(comparison, ConfirmationMetric.Median, 0.35);
        AssertFlagged(comparison, ConfirmationMetric.Mean, 0.05);
        AssertFlagged(comparison, ConfirmationMetric.P95, 0.05);
    }

    [Fact]
    public void ComparisonResult_WithoutFlagged_ReturnsEmpty()
    {
        Assert.Empty(new BenchmarkComparisonResult(CompareSucceeded: true, RegressionDetected: false).FlaggedOrEmpty);
    }

    [Fact]
    public async Task CompareBenchmarks_WithFlaggedOutput_WritesFlaggedFile()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: 10_000_000, meanNs: 10_000_000, p95Ns: 10_000_000, BenchmarkTestData.CreateMeasurements(10_000_000, 10_100_000, 9_900_000, 10_000_000, 10_050_000)));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: 20_000_000, meanNs: 20_000_000, p95Ns: 20_000_000, BenchmarkTestData.CreateMeasurements(20_000_000, 20_100_000, 19_900_000, 20_000_000, 20_050_000)));
        using RoundsRoot root = new();
        string path = Path.Combine(root.Path, "flagged.json");

        BenchmarkComparisonResult comparison = await PerfDiff.CompareBenchmarks(path)(baseline.Path, results.Path, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);

        Assert.True(comparison.RegressionDetected);
        Assert.Equal(comparison.FlaggedOrEmpty, await ConfirmationHypothesisFile.ReadAsync(path, CancellationToken.None).ConfigureAwait(true));
    }

    [Fact]
    public async Task CompareBenchmarks_WhenComparisonFails_DoesNotWriteFlaggedFile()
    {
        using RoundsRoot root = new();
        string path = Path.Combine(root.Path, "flagged.json");

        BenchmarkComparisonResult comparison = await PerfDiff.CompareBenchmarks(path)(root.Path, root.Path, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);

        Assert.False(comparison.CompareSucceeded);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ProgramMain_WithFlaggedOutput_WritesEmptyFlaggedFile()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        string path = Path.Combine(results.Path, "flagged.out");

        int exitCode = await DiffCommand.CreateCommandLineOptions()
            .Parse(["--baseline", baseline.Path, "--results", results.Path, "--flaggedOutput", path, "-v", "quiet"])
            .InvokeAsync()
            .ConfigureAwait(true);

        Assert.Equal(0, exitCode);
        Assert.Empty(await ConfirmationHypothesisFile.ReadAsync(path, CancellationToken.None).ConfigureAwait(true));
    }

    private static void AssertFlagged(BenchmarkComparisonResult comparison, ConfirmationMetric metric, double minimumEffect)
    {
        ConfirmationHypothesis hypothesis = Assert.Single(comparison.FlaggedOrEmpty, flagged => flagged.Metric == metric);
        Assert.Equal(BenchmarkId, hypothesis.BenchmarkId);
        Assert.Equal(minimumEffect, hypothesis.MinimumEffect, 12);
        Assert.Null(hypothesis.BudgetNs);
    }

    private static async Task<int> RunConfirmAsync(params double[] ratios)
    {
        using RoundsRoot root = new();
        string flagged = await root.WriteFlaggedAsync().ConfigureAwait(true);
        for (int index = 0; index < ratios.Length; index++)
        {
            root.WriteRound("baseline", index + 1, 100_000_000D);
            root.WriteRound("current", index + 1, 100_000_000D * ratios[index]);
        }

        return await ConfirmCommand.RunAsync(new ConfirmOptions(flagged, root.Path, 3, 10), new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);
    }

    private sealed class RoundsRoot : IDisposable
    {
        public RoundsRoot()
        {
            Path = System.IO.Path.Combine(AppContext.BaseDirectory, "PerfDiff.TestData", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void WriteRound(string side, int round, double valueNs, Measurement[]? measurements = null)
        {
            string folder = Directory.CreateDirectory(System.IO.Path.Combine(Path, side, "r" + round, "results")).FullName;
            BdnResult result = new()
            {
                Benchmarks =
                [
                    BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: valueNs, meanNs: valueNs, p95Ns: valueNs, measurements ?? []),
                ],
            };
            File.WriteAllText(System.IO.Path.Combine(folder, "report-full-compressed.json"), JsonSerializer.Serialize(result));
        }

        public async Task<string> WriteFlaggedAsync(string? benchmarkId = null)
        {
            string path = System.IO.Path.Combine(Path, "flagged.json");
            await ConfirmationHypothesisFile.WriteAsync(path, [new ConfirmationHypothesis(benchmarkId ?? BenchmarkId, ConfirmationMetric.Mean, 0.05, BudgetNs: null)], CancellationToken.None).ConfigureAwait(false);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
