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
    public void P95RatioRegressionStrategy_WithInfiniteP95Ratio_ReturnsFalse()
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

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenP95RegressesAndMedianDoesNot_ReturnsTrue()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: Milliseconds(5),
                meanNs: Milliseconds(5),
                p95Ns: Milliseconds(5),
                measurements: BenchmarkTestData.CreateMeasurements(Milliseconds(5), Milliseconds(5), Milliseconds(5), Milliseconds(5), Milliseconds(5))),
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: Milliseconds(5),
                meanNs: Milliseconds(6),
                p95Ns: Milliseconds(6),
                measurements: BenchmarkTestData.CreateMeasurements(Milliseconds(6), Milliseconds(6), Milliseconds(6), Milliseconds(6), Milliseconds(6))));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WithInfiniteMedianRatio_ReturnsFalse()
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

        Assert.False(hasRegression);
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

    [Fact]
    public void MeanPercentageRegressionStrategy_WhenWorseDeltaBelowAbsoluteThreshold_ReturnsFalse()
    {
        // Relative 5% threshold is crossed (100ns vs 200ns), but the absolute mean delta
        // (100ns) stays below the 0.5ms budget, so the worse result is skipped.
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: 100, measurements: BenchmarkTestData.CreateMeasurements(100, 100, 100, 100, 100)),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: 200, measurements: BenchmarkTestData.CreateMeasurements(200, 200, 200, 200, 200)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WhenBetterDeltaBelowAbsoluteThreshold_ReturnsFalse()
    {
        // A "better" result (diff faster) whose absolute mean delta stays below the 0.5ms
        // budget is skipped in the better loop and never reported.
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: 200, measurements: BenchmarkTestData.CreateMeasurements(200, 200, 200, 200, 200)),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: 100, measurements: BenchmarkTestData.CreateMeasurements(100, 100, 100, 100, 100)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenWorseDeltaBelowAbsoluteThreshold_ReturnsFalse()
    {
        // Relative 5% threshold is crossed, but the absolute P95 delta (100ns) stays below
        // the 0.5ms budget, so the worse result is skipped.
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 100, measurements: BenchmarkTestData.CreateMeasurements(100, 100, 100, 100, 100)),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 200, measurements: BenchmarkTestData.CreateMeasurements(200, 200, 200, 200, 200)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenBetterDeltaBelowAbsoluteThreshold_ReturnsFalse()
    {
        // A "better" result whose absolute P95 delta stays below the 0.5ms budget is skipped
        // in the better loop and never reported.
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 200, measurements: BenchmarkTestData.CreateMeasurements(200, 200, 200, 200, 200)),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 100, measurements: BenchmarkTestData.CreateMeasurements(100, 100, 100, 100, 100)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void GetMedianRatio_WhenStatisticsMissing_ReturnsNaN()
    {
        double ratio = BenchmarkDotNetDiffer.GetMedianRatio(ComparisonResult.Greater, new Benchmark(), new Benchmark());

        Assert.True(double.IsNaN(ratio));
    }

    [Fact]
    public void GetMeanRatio_WhenStatisticsMissing_ReturnsNaN()
    {
        double ratio = BenchmarkDotNetDiffer.GetMeanRatio(ComparisonResult.Greater, new Benchmark(), new Benchmark());

        Assert.True(double.IsNaN(ratio));
    }

    [Fact]
    public void GetP95Ratio_WhenConclusionIsLesser_UsesDiffOverBaseline()
    {
        double ratio = BenchmarkDotNetDiffer.GetP95Ratio(
            ComparisonResult.Lesser,
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 10),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 15));

        Assert.Equal(1.5D, ratio);
    }

    [Fact]
    public void GetP95Ratio_WhenConclusionIsGreater_UsesBaselineOverDiff()
    {
        double ratio = BenchmarkDotNetDiffer.GetP95Ratio(
            ComparisonResult.Greater,
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 15),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", p95Ns: 10));

        Assert.Equal(1.5D, ratio);
    }

    [Fact]
    public void GetP95Ratio_WhenStatisticsMissing_ReturnsNaN()
    {
        double ratio = BenchmarkDotNetDiffer.GetP95Ratio(ComparisonResult.Greater, new Benchmark(), new Benchmark());

        Assert.True(double.IsNaN(ratio));
    }

    [Fact]
    public void GetP95Ratio_WhenPercentilesMissing_ReturnsNaN()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark("Benchmark.A");
        benchmark.Statistics!.Percentiles = null;

        double ratio = BenchmarkDotNetDiffer.GetP95Ratio(ComparisonResult.Greater, benchmark, BenchmarkTestData.CreateBenchmark("Benchmark.A"));

        Assert.True(double.IsNaN(ratio));
    }

    [Fact]
    public void GetMeanDelta_WhenStatisticsMissing_ReturnsNaN()
    {
        double delta = BenchmarkDotNetDiffer.GetMeanDelta(ComparisonResult.Greater, new Benchmark(), new Benchmark());

        Assert.True(double.IsNaN(delta));
    }

    [Fact]
    public void GetP95Delta_WhenStatisticsMissing_ReturnsNaN()
    {
        double delta = BenchmarkDotNetDiffer.GetP95Delta(ComparisonResult.Greater, new Benchmark(), new Benchmark());

        Assert.True(double.IsNaN(delta));
    }

    private static RegressionResult CreateRegressionResult(double baseMedian, double diffMedian)
        => new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: baseMedian),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: diffMedian),
            ComparisonResult.Lesser);

    private static double Milliseconds(double value) => value * TimeUnitConstants.NanoSecondsToMilliseconds;
}
