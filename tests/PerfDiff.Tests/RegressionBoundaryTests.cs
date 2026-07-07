using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Perfolizer.Mathematics.Common;
using Xunit;

namespace PerfDiff.Tests;

public sealed class RegressionBoundaryTests
{
    [Fact]
    public void Benchmark_GetOriginalValues_WhenMeasurementsAreNull_ReturnsEmpty()
    {
        Benchmark benchmark = new()
        {
            Measurements = null,
        };

        Assert.Empty(benchmark.GetOriginalValues());
    }

    [Fact]
    public void TryBuildBenchmarkMap_WhenResultEntryIsNull_ReturnsFalse()
    {
        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([null], "baseline", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void TryBuildBenchmarkMap_WhenMeasurementsAreNull_ReturnsFalse()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark("Benchmark.A");
        benchmark.Measurements = null;

        bool succeeded = BenchmarkDotNetDiffer.TryBuildBenchmarkMap([new BdnResult { Benchmarks = [benchmark] }], "baseline", new PerfDiffTestLogger(), out _);

        Assert.False(succeeded);
    }

    [Fact]
    public void FindRegressions_SkipsOnlyBenchmarksWithMissingStatistics()
    {
        BdnComparisonResult missingBaseStatistics = new(
            "Benchmark.MissingBase",
            new Benchmark { FullName = "Benchmark.MissingBase", Measurements = BenchmarkTestData.CreateMeasurements(100, 100) },
            BenchmarkTestData.CreateBenchmark("Benchmark.MissingBase", measurements: BenchmarkTestData.CreateMeasurements(200, 200)));
        BdnComparisonResult missingDiffStatistics = new(
            "Benchmark.MissingDiff",
            BenchmarkTestData.CreateBenchmark("Benchmark.MissingDiff", measurements: BenchmarkTestData.CreateMeasurements(100, 100)),
            new Benchmark { FullName = "Benchmark.MissingDiff", Measurements = BenchmarkTestData.CreateMeasurements(200, 200) });
        BdnComparisonResult valid = new(
            "Benchmark.Valid",
            BenchmarkTestData.CreateBenchmark("Benchmark.Valid", measurements: BenchmarkTestData.CreateMeasurements(100, 100, 100, 100, 100)),
            BenchmarkTestData.CreateBenchmark("Benchmark.Valid", measurements: BenchmarkTestData.CreateMeasurements(300, 300, 300, 300, 300)));

        RegressionResult[] regressions = BenchmarkDotNetDiffer.FindRegressions(
            [missingBaseStatistics, missingDiffStatistics, valid],
            Perfolizer.Metrology.Threshold.Parse("5%"));

        Assert.Single(regressions);
        Assert.Equal("Benchmark.Valid", regressions[0].Id);
    }

    [Fact]
    public void GetP95Delta_WhenOnePercentileSetIsMissing_ReturnsNaN()
    {
        Benchmark baseline = BenchmarkTestData.CreateBenchmark("Benchmark.A");
        Benchmark diff = BenchmarkTestData.CreateBenchmark("Benchmark.A");
        diff.Statistics!.Percentiles = null;

        double delta = BenchmarkDotNetDiffer.GetP95Delta(ComparisonResult.Lesser, baseline, diff);

        Assert.True(double.IsNaN(delta));
    }

    [Fact]
    public void PercentageRegressionStrategy_WhenWorseGeomeanIsUndefined_ReturnsFalse()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyWorseComparison(baseMedianNs: 0, diffMedianNs: 0);

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Equal("Median ratio", details.MetricName);
    }

    [Fact]
    public void PercentageRegressionStrategy_WhenWorseExceedsAbsoluteThreshold_LogsGeomean()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyWorseComparison(baseMedianNs: Milliseconds(5), diffMedianNs: Milliseconds(8));
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression([comparison], logger, out _);

        Assert.True(hasRegression);
        Assert.Contains(logger.Messages, message => message.Contains("worse, geomean", StringComparison.Ordinal));
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WhenWorseGeomeanIsUndefined_ReturnsFalse()
    {
        BdnComparisonResult comparison = CreateStatisticallyWorseComparison(
            baseMedianNs: 0,
            diffMedianNs: 0,
            baseMeanNs: 0,
            diffMeanNs: 1_000_000);

        AssertNoRatioRegression(new MeanPercentageRegressionStrategy(), comparison, "Mean Ratio");
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WhenWorseExceedsAbsoluteThreshold_LogsGeomean()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyWorseComparison(
            baseMedianNs: 100,
            diffMedianNs: 300,
            baseMeanNs: Milliseconds(200),
            diffMeanNs: Milliseconds(300));
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression([comparison], logger, out _);

        Assert.True(hasRegression);
        Assert.Contains(logger.Messages, message => message.Contains("worse, geomean", StringComparison.Ordinal));
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WhenBetterGeomeanIsUndefined_DoesNotReportRegression()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyBetterComparison(
            baseMedianNs: 0,
            diffMedianNs: 0,
            baseMeanNs: Milliseconds(300),
            diffMeanNs: Milliseconds(200));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Equal("Mean Ratio", details.MetricName);
    }

    [Fact]
    public void MeanPercentageRegressionStrategy_WhenBetterExceedsAbsoluteThreshold_LogsGeomean()
    {
        MeanPercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyBetterComparison(
            baseMedianNs: 300,
            diffMedianNs: 100,
            baseMeanNs: Milliseconds(300),
            diffMeanNs: Milliseconds(200));
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression([comparison], logger, out _);

        Assert.False(hasRegression);
        Assert.Contains(logger.Messages, message => message.Contains("better, geomean", StringComparison.Ordinal));
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenWorseGeomeanIsUndefined_ReturnsFalse()
    {
        BdnComparisonResult comparison = CreateStatisticallyWorseComparison(
            baseMedianNs: 0,
            diffMedianNs: 0,
            baseP95Ns: 0,
            diffP95Ns: 1_000_000);

        AssertNoRatioRegression(new P95RatioRegressionStrategy(), comparison, "P95 ratio");
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenWorseExceedsAbsoluteThreshold_LogsGeomean()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyWorseComparison(
            baseMedianNs: 100,
            diffMedianNs: 300,
            baseMeanNs: Milliseconds(200),
            diffMeanNs: Milliseconds(300),
            baseP95Ns: Milliseconds(200),
            diffP95Ns: Milliseconds(300));
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression([comparison], logger, out _);

        Assert.True(hasRegression);
        Assert.Contains(logger.Messages, message => message.Contains("worse, geomean", StringComparison.Ordinal));
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenBetterGeomeanIsUndefined_DoesNotReportRegression()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyBetterComparison(
            baseMedianNs: 0,
            diffMedianNs: 0,
            baseMeanNs: Milliseconds(300),
            diffMeanNs: Milliseconds(200),
            baseP95Ns: Milliseconds(300),
            diffP95Ns: Milliseconds(200));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Equal("P95 ratio", details.MetricName);
    }

    [Fact]
    public void P95RatioRegressionStrategy_WhenBetterExceedsAbsoluteThreshold_LogsGeomean()
    {
        P95RatioRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyBetterComparison(
            baseMedianNs: 300,
            diffMedianNs: 100,
            baseMeanNs: Milliseconds(300),
            diffMeanNs: Milliseconds(200),
            baseP95Ns: Milliseconds(300),
            diffP95Ns: Milliseconds(200));
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression([comparison], logger, out _);

        Assert.False(hasRegression);
        Assert.Contains(logger.Messages, message => message.Contains("better, geomean", StringComparison.Ordinal));
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenWorseGeomeanIsUndefined_StillReportsRegression()
    {
        PercentileRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateAbsoluteThresholdComparison(baseMedianNs: 0, diffMedianNs: 0, baseP95Ns: 100, diffP95Ns: 260_000_000);

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.True(hasRegression);
        Assert.Equal("P95", details.MetricName);
    }

    [Fact]
    public void PercentageRegressionStrategy_WhenBetterGeomeanIsUndefined_DoesNotReportRegression()
    {
        PercentageRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateStatisticallyBetterComparison(baseMedianNs: 0, diffMedianNs: 0);

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Equal("Median ratio", details.MetricName);
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenBetterGeomeanIsUndefined_DoesNotReportRegression()
    {
        PercentileRegressionStrategy strategy = new();
        BdnComparisonResult comparison = CreateAbsoluteThresholdComparison(baseMedianNs: 0, diffMedianNs: 0, baseP95Ns: 260_000_000, diffP95Ns: 100);

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Equal("P95", details.MetricName);
    }

    [Fact]
    public void MeanWallClockRegressionStrategy_WhenMetricIsMissing_ReturnsFalse()
    {
        MeanWallClockRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            new Benchmark { FullName = "Benchmark.A", Statistics = null },
            BenchmarkTestData.CreateBenchmark("Benchmark.A"));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenMetricIsMissing_ReturnsFalse()
    {
        PercentileRegressionStrategy strategy = new();
        Benchmark baseline = BenchmarkTestData.CreateBenchmark("Benchmark.A");
        baseline.Statistics!.Percentiles = null;
        BdnComparisonResult comparison = new("Benchmark.A", baseline, BenchmarkTestData.CreateBenchmark("Benchmark.A"));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenDiffMetricIsMissing_ReturnsFalse()
    {
        PercentileRegressionStrategy strategy = new();
        Benchmark diff = BenchmarkTestData.CreateBenchmark("Benchmark.A");
        diff.Statistics!.Percentiles = null;
        BdnComparisonResult comparison = new("Benchmark.A", BenchmarkTestData.CreateBenchmark("Benchmark.A"), diff);

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenDiffStatisticsAreMissing_ReturnsFalse()
    {
        PercentileRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A"),
            new Benchmark { FullName = "Benchmark.A", Statistics = null });

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    private static BdnComparisonResult CreateStatisticallyWorseComparison(
        double baseMedianNs,
        double diffMedianNs,
        double baseMeanNs = 100,
        double diffMeanNs = 300,
        double baseP95Ns = 100,
        double diffP95Ns = 300)
        => new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: baseMedianNs,
                meanNs: baseMeanNs,
                p95Ns: baseP95Ns,
                measurements: BenchmarkTestData.CreateMeasurements(100, 100, 100, 100, 100)),
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: diffMedianNs,
                meanNs: diffMeanNs,
                p95Ns: diffP95Ns,
                measurements: BenchmarkTestData.CreateMeasurements(300, 300, 300, 300, 300)));

    private static BdnComparisonResult CreateStatisticallyBetterComparison(
        double baseMedianNs,
        double diffMedianNs,
        double baseMeanNs = 300,
        double diffMeanNs = 100,
        double baseP95Ns = 300,
        double diffP95Ns = 100)
        => new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: baseMedianNs,
                meanNs: baseMeanNs,
                p95Ns: baseP95Ns,
                measurements: BenchmarkTestData.CreateMeasurements(300, 300, 300, 300, 300)),
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                medianNs: diffMedianNs,
                meanNs: diffMeanNs,
                p95Ns: diffP95Ns,
                measurements: BenchmarkTestData.CreateMeasurements(100, 100, 100, 100, 100)));

    private static BdnComparisonResult CreateAbsoluteThresholdComparison(
        double baseMedianNs,
        double diffMedianNs,
        double baseP95Ns,
        double diffP95Ns)
        => new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: baseMedianNs, p95Ns: baseP95Ns),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", medianNs: diffMedianNs, p95Ns: diffP95Ns));

    private static void AssertNoRatioRegression(IBenchmarkRegressionStrategy strategy, BdnComparisonResult comparison, string metricName)
    {
        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Equal(metricName, details.MetricName);
    }

    private static double Milliseconds(double value) => value * TimeUnitConstants.NanoSecondsToMilliseconds;
}
