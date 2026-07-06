using System.Diagnostics;
using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using Xunit;

namespace PerfDiff.Tests;

public sealed class InputRobustnessTests
{
    [Fact]
    public async Task TryGetBdnResultAsync_WithNullJson_ReturnsFailure()
    {
        using BenchmarkTestData.ResultDirectory directory = new();
        await File.WriteAllTextAsync(directory.ResultFilePath, "null");

        BdnResults results = await BenchmarkFileReader.TryGetBdnResultAsync([directory.ResultFilePath], new PerfDiffTestLogger(), CancellationToken.None);

        Assert.False(results.Success);
        Assert.Null(results.Results[0]);
    }

    [Fact]
    public async Task TryGetBdnResultAsync_WithMalformedJson_ReturnsFailure()
    {
        using BenchmarkTestData.ResultDirectory directory = new();
        await File.WriteAllTextAsync(directory.ResultFilePath, "{");

        BdnResults results = await BenchmarkFileReader.TryGetBdnResultAsync([directory.ResultFilePath], new PerfDiffTestLogger(), CancellationToken.None);

        Assert.False(results.Success);
        Assert.Null(results.Results[0]);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WithZeroOperations_ReturnsFalse()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark(
            "Benchmark.A",
            100,
            100,
            100,
            BenchmarkTestData.CreateMeasurement(nanoseconds: 100, operations: 0),
            BenchmarkTestData.CreateMeasurement(nanoseconds: 100, operations: 1));

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([new BdnResult { Benchmarks = [benchmark] }], "results", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WithSingleResultMeasurement_ReturnsFalse()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark(
            "Benchmark.A",
            measurements: BenchmarkTestData.CreateMeasurement(nanoseconds: 100, operations: 1));

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([new BdnResult { Benchmarks = [benchmark] }], "results", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WithMissingFullName_ReturnsFalse()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark(string.Empty);

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([new BdnResult { Benchmarks = [benchmark] }], "results", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WithNoResultMeasurements_ReturnsFalse()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark(
            "Benchmark.A",
            measurements: new Measurement
            {
                IterationStage = "Warmup",
                Operations = 1,
                Nanoseconds = 100,
            });

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([new BdnResult { Benchmarks = [benchmark] }], "results", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WithDuplicateFullNames_ReturnsFalse()
    {
        BdnResult result = new()
        {
            Benchmarks =
            [
                BenchmarkTestData.CreateBenchmark("Benchmark.A"),
                BenchmarkTestData.CreateBenchmark("Benchmark.A"),
            ],
        };

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([result], "results", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WithEmptyBenchmarkSet_ReturnsFalse()
    {
        BdnResult result = new()
        {
            Benchmarks = [],
        };

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([result], "results", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WithValidBenchmark_ReturnsTrue()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark("Benchmark.A");

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([new BdnResult { Benchmarks = [benchmark] }], "results", new PerfDiffTestLogger(), out Dictionary<string, Benchmark>? benchmarks);

        Assert.True(succeeded);
        Assert.NotNull(benchmarks);
        Assert.Same(benchmark, benchmarks["Benchmark.A"]);
    }

    [Fact]
    public void GetOriginalValues_UsesOnlyResultMeasurementsAndDividesByOperations()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark(
            "Benchmark.A",
            100,
            100,
            100,
            BenchmarkTestData.CreateMeasurement(nanoseconds: 200, operations: 2),
            new Measurement
            {
                IterationStage = "Warmup",
                Operations = 1,
                Nanoseconds = 1_000,
            });

        double[] values = benchmark.GetOriginalValues();

        Assert.Equal([100], values);
    }

    [Fact]
    public void FindRegressions_WithSingleSampleMeasurements_ReturnsEmpty()
    {
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", measurements: BenchmarkTestData.CreateMeasurement(nanoseconds: 100, operations: 1)),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", measurements: BenchmarkTestData.CreateMeasurement(nanoseconds: 200, operations: 1)));

        RegressionResult[] regressions = BenchmarkDotNetDiffer.FindRegressions([comparison], Perfolizer.Metrology.Threshold.Parse("5%"));

        Assert.Empty(regressions);
    }

    [Fact]
    public async Task TryCompareBenchmarkDotNetResultsAsync_WithValidFolders_ReportsSuccess()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));

        BenchmarkComparisonResult comparison = await BenchmarkDotNetDiffer.TryCompareBenchmarkDotNetResultsAsync(baseline.Path, results.Path, new PerfDiffTestLogger(), CancellationToken.None);

        Assert.True(comparison.CompareSucceeded);
        Assert.False(comparison.RegressionDetected);
    }

    [Fact]
    public async Task TryCompareBenchmarkDotNetResultsAsync_WithMissingBaselineFolder_ReportsFailure()
    {
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        string missingBaseline = Path.Combine(AppContext.BaseDirectory, "PerfDiff.TestData", Guid.NewGuid().ToString("N"));

        BenchmarkComparisonResult comparison = await BenchmarkDotNetDiffer.TryCompareBenchmarkDotNetResultsAsync(missingBaseline, results.Path, new PerfDiffTestLogger(), CancellationToken.None);

        Assert.False(comparison.CompareSucceeded);
        Assert.False(comparison.RegressionDetected);
    }

    [Fact]
    public async Task TryGetBdnResultsAsync_AcceptsSingleFilePath()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));

        BdnComparisonResult[]? comparison = await BenchmarkDotNetDiffer.TryGetBdnResultsAsync(baseline.ResultFilePath, results.Path, new PerfDiffTestLogger(), CancellationToken.None);

        Assert.NotNull(comparison);
        Assert.Single(comparison);
    }

    [Fact]
    public async Task TryGetBdnResultsAsync_WhenBaselineFileUnreadable_ReturnsNull()
    {
        using BenchmarkTestData.ResultDirectory baseline = new();
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        await File.WriteAllTextAsync(baseline.ResultFilePath, "{");

        BdnComparisonResult[]? comparison = await BenchmarkDotNetDiffer.TryGetBdnResultsAsync(baseline.Path, results.Path, new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Null(comparison);
    }

    [Fact]
    public async Task TryGetBdnResultsAsync_WhenResultsFileUnreadable_ReturnsNull()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = new();
        await File.WriteAllTextAsync(results.ResultFilePath, "{");

        BdnComparisonResult[]? comparison = await BenchmarkDotNetDiffer.TryGetBdnResultsAsync(baseline.Path, results.Path, new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Null(comparison);
    }

    [Fact]
    public void GetOriginalValues_SkipsNonPositiveOperationMeasurements()
    {
        // A Result-stage measurement with non-positive operations is rejected by upstream
        // validation, so the defensive skip in GetOriginalValues is guarded by Debug.Assert.
        // Suppress the assertion listener to exercise the release-mode fallback: the invalid
        // measurement is skipped and only the valid measurement contributes a value.
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark(
            "Benchmark.A",
            100,
            100,
            100,
            new Measurement { IterationStage = "Result", Operations = 0, Nanoseconds = 100 },
            BenchmarkTestData.CreateMeasurement(nanoseconds: 200, operations: 2));

        double[] values = WithoutAssertListeners(benchmark.GetOriginalValues);

        Assert.Equal([100], values);
    }

    private static T WithoutAssertListeners<T>(Func<T> action)
    {
        TraceListener[] saved = new TraceListener[Trace.Listeners.Count];
        Trace.Listeners.CopyTo(saved, 0);
        Trace.Listeners.Clear();
        try
        {
            return action();
        }
        finally
        {
            Trace.Listeners.Clear();
            Trace.Listeners.AddRange(saved);
        }
    }
}
