using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Perfolizer.Mathematics.Common;
using Xunit;

namespace PerfDiff.Tests;

public sealed class RatioRegressionStrategyTests
{
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

        Assert.True(BenchmarkDotNetDiffer.GetMedianRatio(ComparisonResult.Lesser, comparison.BaseResult, comparison.DiffResult) > PercentageRegressionStrategy.MedianAggregateRatioRegressionThreshold);
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
    public void P95RatioRegressionStrategy_WhenP95RegressesButMeanDeltaIsBelowNoiseBand_ReturnsFalse()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(0.5),
                meanNs: Milliseconds(0.50),
                p95Ns: Milliseconds(0.50)),
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(0.7),
                meanNs: Milliseconds(0.55),
                p95Ns: Milliseconds(1.10)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(BenchmarkDotNetDiffer.GetP95Delta(ComparisonResult.Lesser, comparison.BaseResult, comparison.DiffResult) > RegressionStrategyHelper.AbsoluteNoiseFloorNs);
        Assert.False(RegressionStrategyHelper.ExceedsRatioNoiseFloor(
            new RegressionResult("Benchmark.A", comparison.BaseResult, comparison.DiffResult, ComparisonResult.Lesser),
            result => BenchmarkDotNetDiffer.GetMeanDelta(result.Conclusion, result.BaseResult, result.DiffResult)));
        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenP95AndMeanRegressBeyondNoiseBand_ReturnsTrue()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(5),
                meanNs: Milliseconds(5),
                p95Ns: Milliseconds(5),
                standardDeviationNs: Milliseconds(0.05)),
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(6),
                meanNs: Milliseconds(6),
                p95Ns: Milliseconds(6),
                standardDeviationNs: Milliseconds(0.05)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenPercentilesMissingButMeanDeltaExceedsFloor_ReturnsFalseWithoutThrowing()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(5),
                meanNs: Milliseconds(5),
                p95Ns: Milliseconds(5)),
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(6),
                meanNs: Milliseconds(6),
                p95Ns: Milliseconds(6),
                hasPercentiles: false));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(BenchmarkDotNetDiffer.GetMeanDelta(ComparisonResult.Lesser, comparison.BaseResult, comparison.DiffResult) > RegressionStrategyHelper.AbsoluteNoiseFloorNs);
        Assert.True(double.IsNaN(BenchmarkDotNetDiffer.GetP95Delta(ComparisonResult.Lesser, comparison.BaseResult, comparison.DiffResult)));
        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenMeanDeltaEqualsNoiseBand_ReturnsFalse()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(10),
                meanNs: Milliseconds(10),
                p95Ns: Milliseconds(10),
                standardDeviationNs: Milliseconds(1)),
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(12),
                meanNs: Milliseconds(12),
                p95Ns: Milliseconds(12)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenP95AggregateGeomeanEqualsThreshold_ReturnsFalse()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateComparison(
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(10),
                meanNs: Milliseconds(10),
                p95Ns: Milliseconds(10)),
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(11),
                meanNs: Milliseconds(11),
                p95Ns: Milliseconds(10.5)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WithP95CorroborationFixtures_KeepsMeanBehavior()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult meanRegression = CreateComparison(
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(5),
                meanNs: Milliseconds(5),
                p95Ns: Milliseconds(5),
                standardDeviationNs: Milliseconds(0.05)),
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(6),
                meanNs: Milliseconds(6),
                p95Ns: Milliseconds(6),
                standardDeviationNs: Milliseconds(0.05)));
        BdnComparisonResult meanNoise = CreateComparison(
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(0.5),
                meanNs: Milliseconds(0.50),
                p95Ns: Milliseconds(0.50)),
            BenchmarkWithMeasuredStatistics(
                measurementNs: Milliseconds(0.7),
                meanNs: Milliseconds(0.55),
                p95Ns: Milliseconds(1.10)));

        bool regressionDetected = strategy.HasRegression([meanRegression], new PerfDiffTestLogger(), out _);
        bool noiseDetected = strategy.HasRegression([meanNoise], new PerfDiffTestLogger(), out _);

        Assert.True(regressionDetected);
        Assert.False(noiseDetected);
    }

    [Fact]
    public void RegressionRatioMetricConfig_WhenStabilityDeltaSelectorIsOmitted_UsesDeltaSelector()
    {
        Func<RegressionResult, double> deltaSelector = _ => 42D;
        RegressionRatioMetricConfig config = new("Test ratio", _ => 1D, deltaSelector);

        Assert.Same(deltaSelector, config.StabilityDeltaSelector);
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

    private static Benchmark BenchmarkWithMeasuredStatistics(
        double measurementNs,
        double meanNs,
        double p95Ns,
        double standardDeviationNs = 0,
        bool hasPercentiles = true)
        => new()
        {
            FullName = "Benchmark.A",
            Statistics = new Statistics
            {
                N = 5,
                Median = measurementNs,
                Mean = meanNs,
                StandardDeviation = standardDeviationNs,
                Percentiles = hasPercentiles
                    ? new Percentiles
                    {
                        P95 = p95Ns,
                    }
                    : null,
            },
            Measurements = BenchmarkTestData.CreateMeasurements(measurementNs, measurementNs, measurementNs, measurementNs, measurementNs),
        };

    private static double Milliseconds(double value) => value * TimeUnitConstants.NanoSecondsToMilliseconds;
}
