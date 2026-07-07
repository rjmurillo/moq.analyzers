using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Perfolizer.Mathematics.Common;
using Xunit;

namespace PerfDiff.Tests;

public sealed class AggregateNoiseFloorRegressionStrategyTests
{
    public static IEnumerable<object[]> RatioStrategies()
    {
        yield return [new MeanPercentageRegressionStrategy()];
        yield return [new P95RatioRegressionStrategy()];
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithStableLargeAggregateRegression_ReturnsTrue(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult comparison = CreateComparison(
            "Benchmark.TrueRegression",
            baseMeanNs: Milliseconds(200),
            diffMeanNs: Milliseconds(230),
            baseP95Ns: Milliseconds(220),
            diffP95Ns: Milliseconds(260),
            baseMeasurements: MeasurementsInMilliseconds(200, 200, 200, 200, 200),
            diffMeasurements: MeasurementsInMilliseconds(230, 230, 230, 230, 230));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithSubStabilityFloorNoise_ReturnsFalse(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult comparison = CreateComparison(
            "Benchmark.Noisy",
            baseMeanNs: Milliseconds(2),
            diffMeanNs: Milliseconds(2.8),
            baseP95Ns: Milliseconds(3),
            diffP95Ns: Milliseconds(3.8),
            baseMeasurements: MeasurementsInMilliseconds(2, 2, 2, 2, 2),
            diffMeasurements: MeasurementsInMilliseconds(2.8, 2.8, 2.8, 2.8, 2.8));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithNoisyWorseBenchmarkAndNetImprovement_ReturnsFalse(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult noisyWorse = CreateComparison(
            "Benchmark.Moq1000WithDiagnostics",
            baseMeanNs: Milliseconds(4.5),
            diffMeanNs: Milliseconds(5.1),
            baseP95Ns: Milliseconds(6),
            diffP95Ns: Milliseconds(6.7),
            baseMeasurements: MeasurementsInMilliseconds(4.5, 4.5, 4.5, 4.5, 4.5),
            diffMeasurements: MeasurementsInMilliseconds(5.1, 5.1, 5.1, 5.1, 5.1));
        BdnComparisonResult stableBetter = CreateComparison(
            "Benchmark.StableImprovement",
            baseMeanNs: Milliseconds(200),
            diffMeanNs: Milliseconds(180),
            baseP95Ns: Milliseconds(220),
            diffP95Ns: Milliseconds(190),
            baseMeasurements: MeasurementsInMilliseconds(200, 200, 200, 200, 200),
            diffMeasurements: MeasurementsInMilliseconds(180, 180, 180, 180, 180));

        bool hasRegression = strategy.HasRegression([noisyWorse, stableBetter], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void HasRegression_WithEmptyComparison_ReturnsFalse()
    {
        MeanPercentageRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression([], new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Equal("Mean Ratio", details.MetricName);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenDeltaEqualsAbsoluteFloor_ReturnsFalse()
    {
        RegressionResult result = CreateRegressionResult(Milliseconds(200), Milliseconds(201));

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => RegressionStrategyHelper.AbsoluteNoiseFloorNs);

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenBaselineEqualsStabilityFloor_ReturnsTrue()
    {
        RegressionResult result = CreateRegressionResult(RegressionStrategyHelper.BenchmarkDotNetStabilityFloorNs, Milliseconds(120));

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.True(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenBaselineIsBelowStabilityFloor_ReturnsFalse()
    {
        RegressionResult result = CreateRegressionResult(Milliseconds(99.999), Milliseconds(120));

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenDeltaIsWithinCombinedStandardDeviation_ReturnsFalse()
    {
        RegressionResult result = CreateRegressionResult(
            Milliseconds(200),
            Milliseconds(220),
            baseStandardDeviationNs: Milliseconds(8),
            diffStandardDeviationNs: Milliseconds(8),
            n: 10);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenDeltaExceedsCombinedStandardDeviation_ReturnsTrue()
    {
        RegressionResult result = CreateRegressionResult(
            Milliseconds(200),
            Milliseconds(240),
            baseStandardDeviationNs: Milliseconds(4),
            diffStandardDeviationNs: Milliseconds(4),
            n: 10);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(40));

        Assert.True(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenStandardDeviationIsUnavailable_UsesAbsoluteFloor()
    {
        RegressionResult result = CreateRegressionResult(Milliseconds(200), Milliseconds(220), n: 0);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.True(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenStandardErrorIsAvailable_UsesDerivedStandardDeviation()
    {
        RegressionResult result = CreateRegressionResult(
            Milliseconds(200),
            Milliseconds(220),
            baseStandardDeviationNs: double.NaN,
            diffStandardDeviationNs: double.NaN,
            baseStandardErrorNs: Milliseconds(4),
            diffStandardErrorNs: Milliseconds(4),
            n: 4);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenConfidenceIntervalStandardErrorIsAvailable_UsesDerivedStandardDeviation()
    {
        RegressionResult result = CreateRegressionResult(
            Milliseconds(200),
            Milliseconds(220),
            baseStandardDeviationNs: double.NaN,
            diffStandardDeviationNs: double.NaN,
            baseStandardErrorNs: double.NaN,
            diffStandardErrorNs: double.NaN,
            n: 0,
            confidenceIntervalN: 4,
            confidenceIntervalStandardErrorNs: Milliseconds(4));

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenDeltaIsNaN_ReturnsFalse()
    {
        RegressionResult result = CreateRegressionResult(Milliseconds(200), Milliseconds(220));

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => double.NaN);

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenBaselineStatisticsAreMissing_ReturnsFalse()
    {
        RegressionResult result = new(
            "Benchmark.MissingBaseStats",
            new Benchmark(),
            CreateBenchmark("Benchmark.MissingBaseStats", Milliseconds(220), Milliseconds(220), Milliseconds(240)),
            ComparisonResult.Lesser);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenDiffStatisticsAreMissing_FallsBackToAbsoluteFloor()
    {
        RegressionResult result = new(
            "Benchmark.MissingDiffStats",
            CreateBenchmark("Benchmark.MissingDiffStats", Milliseconds(200), Milliseconds(200), Milliseconds(220), standardDeviationNs: Milliseconds(1), n: 2),
            new Benchmark(),
            ComparisonResult.Lesser);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.True(exceeds);
    }

    [Fact]
    public void AbsoluteRegressionStrategy_WhenWorseGeomeanIsUndefined_ReturnsTrue()
    {
        MeanWallClockRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.Absolute",
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: 0, meanNs: Milliseconds(50)),
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: 0, meanNs: Milliseconds(120)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void AbsoluteRegressionStrategy_WhenWorseGeomeanIsDefined_LogsGeomean()
    {
        MeanWallClockRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.Absolute",
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: Milliseconds(50), meanNs: Milliseconds(50)),
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: Milliseconds(120), meanNs: Milliseconds(120)));
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression([comparison], logger, out _);

        Assert.True(hasRegression);
        Assert.Contains(logger.Messages, message => message.Contains("worse, geomean", StringComparison.Ordinal));
    }

    [Fact]
    public void AbsoluteRegressionStrategy_WhenBetterGeomeanIsUndefined_ReturnsFalse()
    {
        MeanWallClockRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.Absolute",
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: 0, meanNs: Milliseconds(120)),
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: 0, meanNs: Milliseconds(50)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void AbsoluteRegressionStrategy_WhenBetterGeomeanIsDefined_LogsGeomean()
    {
        MeanWallClockRegressionStrategy strategy = new();
        BdnComparisonResult comparison = new(
            "Benchmark.Absolute",
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: Milliseconds(120), meanNs: Milliseconds(120)),
            BenchmarkTestData.CreateBenchmark("Benchmark.Absolute", medianNs: Milliseconds(50), meanNs: Milliseconds(50)));
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression([comparison], logger, out _);

        Assert.False(hasRegression);
        Assert.Contains(logger.Messages, message => message.Contains("better, geomean", StringComparison.Ordinal));
    }

    [Fact]
    public void IsAggregateRatioRegression_WhenGeomeanEqualsFivePercent_ReturnsFalse()
    {
        RegressionResult[] results = [CreateRegressionResult(Milliseconds(200), Milliseconds(210))];

        bool hasRegression = RegressionStrategyHelper.IsAggregateRatioRegression(results, _ => 1.05D, out double geomean);

        Assert.False(hasRegression);
        Assert.Equal(1.05D, geomean);
    }

    [Fact]
    public void IsAggregateRatioRegression_WhenGeomeanExceedsFivePercent_ReturnsTrue()
    {
        RegressionResult[] results = [CreateRegressionResult(Milliseconds(200), Milliseconds(220))];

        bool hasRegression = RegressionStrategyHelper.IsAggregateRatioRegression(results, _ => 1.10D, out double geomean);

        Assert.True(hasRegression);
        Assert.Equal(1.10D, geomean);
    }

    [Fact]
    public void IsAggregateRatioRegression_WhenAllRatiosAreInfinite_ReturnsFalse()
    {
        RegressionResult[] results = [CreateRegressionResult(0, Milliseconds(220))];

        bool hasRegression = RegressionStrategyHelper.IsAggregateRatioRegression(results, _ => double.PositiveInfinity, out double geomean);

        Assert.False(hasRegression);
        Assert.True(double.IsNaN(geomean));
    }

    [Fact]
    public void TryGetGeometricMean_WhenRatioIsZero_ReturnsFalse()
    {
        RegressionResult[] results = [CreateRegressionResult(Milliseconds(200), Milliseconds(220))];

        bool succeeded = RegressionStrategyHelper.TryGetGeometricMean(results, _ => 0, out double geomean);

        Assert.False(succeeded);
        Assert.True(double.IsNaN(geomean));
    }

    private static RegressionResult CreateRegressionResult(
        double baseMeanNs,
        double diffMeanNs,
        double baseStandardDeviationNs = 0,
        double diffStandardDeviationNs = 0,
        double baseStandardErrorNs = 0,
        double diffStandardErrorNs = 0,
        int n = 0,
        int confidenceIntervalN = 0,
        double confidenceIntervalStandardErrorNs = 0)
        => new(
            "Benchmark.A",
            CreateBenchmark("Benchmark.A", baseMeanNs, baseMeanNs, baseMeanNs, baseStandardDeviationNs, baseStandardErrorNs, n, confidenceIntervalN, confidenceIntervalStandardErrorNs),
            CreateBenchmark("Benchmark.A", diffMeanNs, diffMeanNs, diffMeanNs, diffStandardDeviationNs, diffStandardErrorNs, n, confidenceIntervalN, confidenceIntervalStandardErrorNs),
            ComparisonResult.Lesser);

    private static BdnComparisonResult CreateComparison(
        string id,
        double baseMeanNs,
        double diffMeanNs,
        double baseP95Ns,
        double diffP95Ns,
        Measurement[] baseMeasurements,
        Measurement[] diffMeasurements)
        => new(
            id,
            CreateBenchmark(id, baseMeanNs, baseMeanNs, baseP95Ns, measurements: baseMeasurements),
            CreateBenchmark(id, diffMeanNs, diffMeanNs, diffP95Ns, measurements: diffMeasurements));

    private static Benchmark CreateBenchmark(
        string id,
        double medianNs,
        double meanNs,
        double p95Ns,
        double standardDeviationNs = 0,
        double standardErrorNs = 0,
        int n = 0,
        int confidenceIntervalN = 0,
        double confidenceIntervalStandardErrorNs = 0,
        Measurement[]? measurements = null)
        => new()
        {
            FullName = id,
            Statistics = new Statistics
            {
                N = n,
                Median = medianNs,
                Mean = meanNs,
                StandardDeviation = standardDeviationNs,
                StandardError = standardErrorNs,
                ConfidenceInterval = confidenceIntervalN > 0
                    ? new global::PerfDiff.BDN.DataContracts.ConfidenceInterval
                    {
                        N = confidenceIntervalN,
                        StandardError = confidenceIntervalStandardErrorNs,
                    }
                    : null,
                Percentiles = new Percentiles
                {
                    P95 = p95Ns,
                },
            },
            Measurements = measurements ?? MeasurementsInMilliseconds(meanNs / TimeUnitConstants.NanoSecondsToMilliseconds),
        };

    private static Measurement[] MeasurementsInMilliseconds(params double[] values)
        => values.Select(value => new Measurement
        {
            IterationStage = "Result",
            Operations = 1,
            Nanoseconds = Milliseconds(value),
        }).ToArray();

    private static double Milliseconds(double value) => value * TimeUnitConstants.NanoSecondsToMilliseconds;
}
