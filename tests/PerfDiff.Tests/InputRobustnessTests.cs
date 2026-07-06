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
}
