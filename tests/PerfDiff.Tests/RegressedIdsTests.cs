using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Xunit;

namespace PerfDiff.Tests;

/// <summary>
/// Covers the ids each strategy reports as the cause of a tripped gate, and the JSON file that carries them.
/// </summary>
public sealed class RegressedIdsTests
{
    private static readonly double OverBudgetNs = 300_000_000D;
    private static readonly double UnderBudgetNs = 10_000_000D;

    [Fact]
    public void PercentileStrategy_WhenGateTrips_ReturnsOnlyWorseIdsSortedOrdinal()
    {
        BdnComparisonResult[] comparison =
        [
            AbsoluteComparison("Zeta", UnderBudgetNs, OverBudgetNs),
            AbsoluteComparison("Alpha", UnderBudgetNs, OverBudgetNs),
            AbsoluteComparison("Stable", UnderBudgetNs, UnderBudgetNs),
            AbsoluteComparison("Improved", OverBudgetNs, UnderBudgetNs),
        ];

        bool hasRegression = new PercentileRegressionStrategy().HasRegression(comparison, new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.True(hasRegression);
        Assert.Equal(["Alpha", "Zeta"], details.RegressedIds);
    }

    [Fact]
    public void PercentileStrategy_WhenDuplicateIdsRegress_ReturnsEachIdOnce()
    {
        BdnComparisonResult[] comparison =
        [
            AbsoluteComparison("Alpha", UnderBudgetNs, OverBudgetNs),
            AbsoluteComparison("Alpha", UnderBudgetNs, OverBudgetNs),
        ];

        new MeanWallClockRegressionStrategy().HasRegression(comparison, new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.Equal(["Alpha"], details.RegressedIds);
    }

    [Fact]
    public void PercentileStrategy_WhenNothingRegresses_ReturnsNoIds()
    {
        BdnComparisonResult[] comparison = [AbsoluteComparison("Alpha", UnderBudgetNs, UnderBudgetNs)];

        bool hasRegression = new PercentileRegressionStrategy().HasRegression(comparison, new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Empty(details.RegressedIds);
    }

    [Fact]
    public void RegressionDetectionResult_WithoutIds_DefaultsToEmpty()
    {
        RegressionDetectionResult details = new("Metric", Perfolizer.Metrology.Threshold.Parse("5%"));

        Assert.Empty(details.RegressedIds);
    }

    [Fact]
    public void P95RatioStrategy_WhenAggregateTrips_ReturnsStableWorseIds()
    {
        BdnComparisonResult[] comparison =
        [
            RatioComparison("Zeta", 5, 6),
            RatioComparison("Alpha", 5, 6),
        ];

        bool hasRegression = new P95RatioRegressionStrategy().HasRegression(comparison, new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.True(hasRegression);
        Assert.Equal(["Alpha", "Zeta"], details.RegressedIds);
    }

    [Fact]
    public void P95RatioStrategy_WhenAggregateDoesNotTrip_ReturnsNoIds()
    {
        BdnComparisonResult[] comparison = [RatioComparison("Alpha", 5, 5)];

        bool hasRegression = new P95RatioRegressionStrategy().HasRegression(comparison, new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Empty(details.RegressedIds);
    }

    [Fact]
    public async Task ComparisonService_UnionsIdsOfTrippedStrategiesSortedOrdinal()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(
            StableBenchmark("Zeta", UnderBudgetNs),
            StableBenchmark("Alpha", UnderBudgetNs),
            StableBenchmark("Quiet", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(
            StableBenchmark("Zeta", OverBudgetNs),
            StableBenchmark("Alpha", OverBudgetNs),
            StableBenchmark("Quiet", UnderBudgetNs));
        BenchmarkComparisonService service = new(new PerfDiffTestLogger());

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None);

        Assert.True(comparison.RegressionDetected);
        Assert.Equal(["Alpha", "Zeta"], comparison.RegressedIds);
    }

    [Fact]
    public async Task ComparisonService_WhenNothingRegresses_ReturnsNoIds()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(StableBenchmark("Alpha", UnderBudgetNs));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(StableBenchmark("Alpha", UnderBudgetNs));
        BenchmarkComparisonService service = new(new PerfDiffTestLogger());

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None);

        Assert.False(comparison.RegressionDetected);
        Assert.Empty(comparison.RegressedIds);
    }

    [Fact]
    public void RegressedIdsFile_Write_SortsAndDeduplicatesOrdinal()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "ids.json");

        RegressedIdsFile.Write(path, ["b", "B", "a", "b", "a"]);

        Assert.Equal("""["B","a","b"]""", File.ReadAllText(path));
    }

    [Fact]
    public void RegressedIdsFile_Write_WithNoIds_WritesEmptyArray()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "ids.json");

        RegressedIdsFile.Write(path, []);

        Assert.Equal("[]", File.ReadAllText(path));
    }

    [Fact]
    public void RegressedIdsFile_Write_CreatesMissingParentFolders()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "nested", "deeper", "ids.json");

        RegressedIdsFile.Write(path, ["a"]);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RegressedIdsFile_Write_WithBareFileName_UsesCurrentDirectory()
    {
        string originalDirectory = Directory.GetCurrentDirectory();
        using TempFolder folder = new();
        try
        {
            Directory.SetCurrentDirectory(folder.Path);

            RegressedIdsFile.Write("bare-ids.json", ["a"]);

            Assert.True(File.Exists(Path.Combine(folder.Path, "bare-ids.json")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void RegressedIdsFile_TryRead_RoundTripsWrittenIds()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "ids.json");
        RegressedIdsFile.Write(path, ["Moq1200.Run(FileCount: 1)", "Moq1000.Run(FileCount: 1000)"]);

        bool read = RegressedIdsFile.TryRead(path, new PerfDiffTestLogger(), out IReadOnlySet<string>? ids);

        Assert.True(read);
        Assert.NotNull(ids);
        Assert.Equal(2, ids.Count);
        Assert.Contains("Moq1200.Run(FileCount: 1)", ids);
    }

    [Fact]
    public void RegressedIdsFile_TryRead_AcceptsEmptyArray()
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "ids.json");
        File.WriteAllText(path, "[]");

        bool read = RegressedIdsFile.TryRead(path, new PerfDiffTestLogger(), out IReadOnlySet<string>? ids);

        Assert.True(read);
        Assert.NotNull(ids);
        Assert.Empty(ids);
    }

    [Fact]
    public void RegressedIdsFile_TryRead_WhenFileIsMissing_ReturnsFalseAndLogs()
    {
        using TempFolder folder = new();
        PerfDiffTestLogger logger = new();

        bool read = RegressedIdsFile.TryRead(Path.Combine(folder.Path, "missing.json"), logger, out IReadOnlySet<string>? ids);

        Assert.False(read);
        Assert.Null(ids);
        Assert.Contains(logger.Messages, message => message.Contains("Cannot read the confirmation file", StringComparison.Ordinal));
    }

    [Fact]
    public void RegressedIdsFile_TryRead_WhenPathIsAFolder_ReturnsFalse()
    {
        using TempFolder folder = new();

        bool read = RegressedIdsFile.TryRead(folder.Path, new PerfDiffTestLogger(), out IReadOnlySet<string>? ids);

        Assert.False(read);
        Assert.Null(ids);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"a":1}""")]
    [InlineData("[1,2]")]
    [InlineData("""["a",""]""")]
    [InlineData("""["a",null]""")]
    [InlineData("null")]
    public void RegressedIdsFile_TryRead_WhenContentIsMalformed_ReturnsFalse(string content)
    {
        using TempFolder folder = new();
        string path = Path.Combine(folder.Path, "ids.json");
        File.WriteAllText(path, content);
        PerfDiffTestLogger logger = new();

        bool read = RegressedIdsFile.TryRead(path, logger, out IReadOnlySet<string>? ids);

        Assert.False(read);
        Assert.Null(ids);
        Assert.NotEmpty(logger.Messages);
    }

    private static BdnComparisonResult AbsoluteComparison(string id, double baselineNs, double currentNs)
        => new(
            id,
            BenchmarkTestData.CreateBenchmark(id, meanNs: baselineNs, p95Ns: baselineNs),
            BenchmarkTestData.CreateBenchmark(id, meanNs: currentNs, p95Ns: currentNs));

    private static BdnComparisonResult RatioComparison(string id, double baseMs, double diffMs)
        => new(
            id,
            RatioBenchmark(id, baseMs),
            RatioBenchmark(id, diffMs));

    private static Benchmark RatioBenchmark(string id, double ms)
    {
        double ns = ms * TimeUnitConstants.NanoSecondsToMilliseconds;
        return BenchmarkTestData.CreateBenchmark(
            id,
            medianNs: ns,
            meanNs: ns,
            p95Ns: ns,
            measurements: BenchmarkTestData.CreateMeasurements(ns, ns, ns, ns, ns));
    }

    private static Benchmark StableBenchmark(string id, double ns)
        => BenchmarkTestData.CreateBenchmark(
            id,
            medianNs: ns,
            meanNs: ns,
            p95Ns: ns,
            measurements: BenchmarkTestData.CreateMeasurements(ns, ns, ns, ns, ns));

    internal sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(AppContext.BaseDirectory, "PerfDiff.TestData", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
