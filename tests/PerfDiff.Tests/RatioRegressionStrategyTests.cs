using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Perfolizer.Mathematics.Common;
using Xunit;

namespace PerfDiff.Tests;

public sealed class RatioRegressionStrategyTests
{
    [Fact]
    public void PercentageRegressionStrategy_WithInfiniteMedianRatio_ReturnsTrue()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: 0, measurements: BenchmarkTestData.CreateMeasurements(0, 0, 0, 0, 0)),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: 10, measurements: BenchmarkTestData.CreateMeasurements(10, 10, 10, 10, 10)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetMedianRatio(ComparisonResult.Lesser, comparison.BaseResult, comparison.DiffResult)));
        Assert.True(hasRegression);
    }

    [Fact]
    public void PercentageRegressionStrategy_WithBetterRatio_ReturnsFalse()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: 10, measurements: BenchmarkTestData.CreateMeasurements(10, 10, 10, 10, 10)),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: 0, measurements: BenchmarkTestData.CreateMeasurements(0, 0, 0, 0, 0)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WithInfiniteMedianRatio_ReturnsTrue()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 0,
                p95Ns: 0,
                measurements: BenchmarkTestData.CreateMeasurements(0, 0, 0, 0, 0)),
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 10,
                p95Ns: 1_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(1_000_000, 1_000_000, 1_000_000, 1_000_000, 1_000_000)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WithInfiniteMedianRatio_ReturnsTrue()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 0,
                meanNs: 0,
                measurements: BenchmarkTestData.CreateMeasurements(0, 0, 0, 0, 0)),
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 10,
                meanNs: 1_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(1_000_000, 1_000_000, 1_000_000, 1_000_000, 1_000_000)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WithBetterRatio_ReturnsFalse()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 1_000_000,
                p95Ns: 1_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(1_000_000, 1_000_000, 1_000_000, 1_000_000, 1_000_000)),
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 0,
                p95Ns: 0,
                measurements: BenchmarkTestData.CreateMeasurements(0, 0, 0, 0, 0)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WithBetterRatio_ReturnsFalse()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 1_000_000,
                meanNs: 1_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(1_000_000, 1_000_000, 1_000_000, 1_000_000, 1_000_000)),
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: 0,
                meanNs: 0,
                measurements: BenchmarkTestData.CreateMeasurements(0, 0, 0, 0, 0)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void TryGetGeometricMean_ExcludesNaNAndDividesByAggregatedCount()
    {
        RegressionResult[] results =
        [
            CreateRegressionResult(baseMedian: 1, diffMedian: 2),
            CreateRegressionResult(baseMedian: 1, diffMedian: 8),
            CreateRegressionResult(baseMedian: 0, diffMedian: 0),
        ];

        bool succeeded = RegressionStrategyHelper.TryGetGeometricMean(results, BenchmarkDotNetDiffer.GetMedianRatio, out double geometricMean);

        Assert.True(succeeded);
        Assert.Equal(4, geometricMean, precision: 12);
    }

    [Fact]
    public void TryGetGeometricMean_WithOnlyNaNRatios_ReturnsFalse()
    {
        RegressionResult[] results =
        [
            CreateRegressionResult(baseMedian: 0, diffMedian: 0),
        ];

        bool succeeded = RegressionStrategyHelper.TryGetGeometricMean(results, BenchmarkDotNetDiffer.GetMedianRatio, out double geometricMean);

        Assert.False(succeeded);
        Assert.True(double.IsNaN(geometricMean));
    }

    private static RegressionResult CreateRegressionResult(double baseMedian, double diffMedian)
        => new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: baseMedian),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: diffMedian),
            ComparisonResult.Lesser);
}
