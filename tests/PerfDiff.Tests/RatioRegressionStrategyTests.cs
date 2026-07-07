using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Perfolizer.Mathematics.Common;
using Xunit;

namespace PerfDiff.Tests;

public sealed class RatioRegressionStrategyTests
{
    private static readonly double MedianAggregateRatioRegressionThreshold = 1.35D;

    [Fact]
    public void PercentageRegressionStrategy_WithInfiniteMedianRatio_ReturnsFalse()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithMedian(0), BenchmarkWithMedian(10));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetMedianRatio(ComparisonResult.Lesser, comparison.BaseResult, comparison.DiffResult)));
        Assert.False(hasRegression);
    }

    [Fact]
    public void PercentageRegressionStrategy_WithStableLargeAggregateRegression_ReturnsTrue()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithMedian(Milliseconds(5)), BenchmarkWithMedian(Milliseconds(7)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void PercentageRegressionStrategy_WhenMedianDeltaIsBelowAbsoluteNoiseFloor_ReturnsFalse()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(
            BenchmarkWithValues(192_800, 181_100, 192_800),
            BenchmarkWithValues(260_800, 261_000, 260_800));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(BenchmarkDotNetDiffer.GetMedianRatio(ComparisonResult.Lesser, comparison.BaseResult, comparison.DiffResult) > MedianAggregateRatioRegressionThreshold);
        Assert.False(hasRegression);
    }

    [Fact]
    public void PercentageRegressionStrategy_WithBetterRatio_ReturnsFalse()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithMedian(10), BenchmarkWithMedian(0));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WithInfiniteP95Ratio_ReturnsFalse()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithP95(0, 0), BenchmarkWithP95(10, 1_000_000));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenP95RegressesAndMedianDoesNot_ReturnsTrue()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(
            BenchmarkWithValues(Milliseconds(5), Milliseconds(5), Milliseconds(5)),
            BenchmarkWithValues(Milliseconds(5), Milliseconds(6), Milliseconds(6)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WithInfiniteMedianRatio_ReturnsFalse()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithMean(0, 0), BenchmarkWithMean(10, 1_000_000));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WithBetterRatio_ReturnsFalse()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithP95(1_000_000, 1_000_000), BenchmarkWithP95(0, 0));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WithBetterRatio_ReturnsFalse()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithMean(1_000_000, 1_000_000), BenchmarkWithMean(0, 0));

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
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithMean(100), BenchmarkWithMean(200));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WhenBetterDeltaBelowAbsoluteThreshold_ReturnsFalse()
    {
        // A "better" result (diff faster) whose absolute mean delta stays below the 0.5ms
        // budget is skipped in the better loop and never reported.
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithMean(200), BenchmarkWithMean(100));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenWorseDeltaBelowAbsoluteThreshold_ReturnsFalse()
    {
        // Relative 5% threshold is crossed, but the absolute P95 delta (100ns) stays below
        // the 0.5ms budget, so the worse result is skipped.
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithP95(100), BenchmarkWithP95(200));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenBetterDeltaBelowAbsoluteThreshold_ReturnsFalse()
    {
        // A "better" result whose absolute P95 delta stays below the 0.5ms budget is skipped
        // in the better loop and never reported.
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(BenchmarkWithP95(200), BenchmarkWithP95(100));

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

    private static BdnComparisonResult CreateComparison(Benchmark baseline, Benchmark diff)
        => new("Benchmark.A", baseline, diff);

    private static Benchmark BenchmarkWithMedian(double medianNs)
        => BenchmarkWithValues(medianNs, medianNs, medianNs);

    private static Benchmark BenchmarkWithMean(double meanNs)
        => BenchmarkWithValues(meanNs, meanNs, meanNs);

    private static Benchmark BenchmarkWithMean(double medianNs, double meanNs)
        => BenchmarkWithValues(medianNs, meanNs, meanNs);

    private static Benchmark BenchmarkWithP95(double p95Ns)
        => BenchmarkWithValues(p95Ns, p95Ns, p95Ns);

    private static Benchmark BenchmarkWithP95(double medianNs, double p95Ns)
        => BenchmarkWithValues(medianNs, p95Ns, p95Ns);

    private static Benchmark BenchmarkWithValues(double medianNs, double meanNs, double p95Ns)
        => BenchmarkTestData.CreateBenchmark(
            "Benchmark.A",
            medianNs: medianNs,
            meanNs: meanNs,
            p95Ns: p95Ns,
            measurements: BenchmarkTestData.CreateMeasurements(meanNs, meanNs, meanNs, meanNs, meanNs));

    private static double Milliseconds(double value) => value * TimeUnitConstants.NanoSecondsToMilliseconds;
}
