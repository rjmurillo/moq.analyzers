using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Perfolizer.Mathematics.Common;
using Xunit;

namespace PerfDiff.Tests;

public sealed class AggregateNoiseFloorRegressionStrategyTests
{
    private static readonly double NoisySubMillisecondMeanMs = 0.4D;
    private static readonly double NoisyRegressionMeanMs = 1.1D;
    private static readonly double LargeStableBaselineMs = 200D;
    private static readonly double LargeStableBelowNoiseMs = 220D;
    private static readonly double LargeStableAboveNoiseMs = 240D;
    private static readonly double TwentyMillisecondDeltaMs = 20D;
    private static readonly double FortyMillisecondDeltaMs = 40D;
    private static readonly double TenPercentRatio = 1.10D;
    private static readonly double MicrosecondsToNanoseconds = 1_000D;

    public static IEnumerable<object[]> RatioStrategies()
    {
        yield return [new MeanPercentageRegressionStrategy()];
        yield return [new P95RatioRegressionStrategy()];
    }

    public static IEnumerable<object[]> StandardDeviationNoiseFloorCases()
    {
        yield return
        [
            new NoiseFloorCase { BaseMeanNs = Milliseconds(NoisySubMillisecondMeanMs), DiffMeanNs = Milliseconds(NoisyRegressionMeanMs), BaseStandardDeviationNs = Milliseconds(0.05), DiffStandardDeviationNs = Milliseconds(0.05), N = 5, DeltaNs = Milliseconds(0.7), Expected = true },
        ];
        yield return
        [
            new NoiseFloorCase { BaseMeanNs = Milliseconds(NoisySubMillisecondMeanMs), DiffMeanNs = Milliseconds(NoisyRegressionMeanMs), BaseStandardDeviationNs = Milliseconds(0.3), DiffStandardDeviationNs = Milliseconds(0.3), N = 5, DeltaNs = Milliseconds(0.7), Expected = false },
        ];
        yield return
        [
            new NoiseFloorCase { BaseMeanNs = Milliseconds(LargeStableBaselineMs), DiffMeanNs = Milliseconds(LargeStableBelowNoiseMs), BaseStandardDeviationNs = Milliseconds(8), DiffStandardDeviationNs = Milliseconds(8), N = 10, DeltaNs = Milliseconds(TwentyMillisecondDeltaMs), Expected = false },
        ];
        yield return
        [
            new NoiseFloorCase { BaseMeanNs = Milliseconds(LargeStableBaselineMs), DiffMeanNs = Milliseconds(LargeStableAboveNoiseMs), BaseStandardDeviationNs = Milliseconds(4), DiffStandardDeviationNs = Milliseconds(4), N = 10, DeltaNs = Milliseconds(FortyMillisecondDeltaMs), Expected = true },
        ];
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithStableLargeAggregateRegression_ReturnsTrue(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult comparison = CreateComparison(
            "Benchmark.TrueRegression",
            BenchmarkCase(Milliseconds(5), Milliseconds(5.2), MeasurementsInMilliseconds(5, 5, 5, 5, 5), Milliseconds(0.05)),
            BenchmarkCase(Milliseconds(6), Milliseconds(6.3), MeasurementsInMilliseconds(6, 6, 6, 6, 6), Milliseconds(0.05)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithSubMillisecondNoise_ReturnsFalse(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult comparison = CreateComparison(
            "Benchmark.Noisy",
            BenchmarkCase(Milliseconds(2), Milliseconds(3), MeasurementsInMilliseconds(2, 2, 2, 2, 2), Milliseconds(0.4)),
            BenchmarkCase(Milliseconds(2.8), Milliseconds(3.8), MeasurementsInMilliseconds(2.8, 2.8, 2.8, 2.8, 2.8), Milliseconds(0.4)));

        bool hasRegression = strategy.HasRegression([comparison], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithNoisyWorseBenchmarkAndNetImprovement_ReturnsFalse(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult noisyWorse = CreateComparison(
            "Benchmark.Moq1000WithDiagnostics",
            BenchmarkCase(Milliseconds(4.5), Milliseconds(6), MeasurementsInMilliseconds(4.5, 4.5, 4.5, 4.5, 4.5), Milliseconds(0.4)),
            BenchmarkCase(Milliseconds(5.1), Milliseconds(6.7), MeasurementsInMilliseconds(5.1, 5.1, 5.1, 5.1, 5.1), Milliseconds(0.4)));
        BdnComparisonResult stableBetter = CreateComparison(
            "Benchmark.StableImprovement",
            BenchmarkCase(Milliseconds(200), Milliseconds(220), MeasurementsInMilliseconds(200, 200, 200, 200, 200)),
            BenchmarkCase(Milliseconds(180), Milliseconds(190), MeasurementsInMilliseconds(180, 180, 180, 180, 180)));

        bool hasRegression = strategy.HasRegression([noisyWorse, stableBetter], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithIssue1315NoisySubMillisecondAggregate_ReturnsFalse(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult firstNoisyBenchmark = CreateComparison(
            "Benchmark.NoisyOne",
            BenchmarkCase(Microseconds(400), Microseconds(500), MeasurementsInMicroseconds(400, 400, 400, 400, 400), Microseconds(300)),
            BenchmarkCase(Microseconds(900), Microseconds(1_000), MeasurementsInMicroseconds(900, 900, 900, 900, 900), Microseconds(300)));
        BdnComparisonResult secondNoisyBenchmark = CreateComparison(
            "Benchmark.NoisyTwo",
            BenchmarkCase(Microseconds(600), Microseconds(700), MeasurementsInMicroseconds(600, 600, 600, 600, 600), Microseconds(325)),
            BenchmarkCase(Microseconds(1_150), Microseconds(1_250), MeasurementsInMicroseconds(1_150, 1_150, 1_150, 1_150, 1_150), Microseconds(325)));

        bool hasRegression = strategy.HasRegression([firstNoisyBenchmark, secondNoisyBenchmark], new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Theory]
    [MemberData(nameof(RatioStrategies))]
    public void HasRegression_WithIssue1317NoiseBandFixtures_ReturnsFalse(IBenchmarkRegressionStrategy strategy)
    {
        BdnComparisonResult moq1000 = CreateComparison(
            "Benchmark.Moq1000WithDiagnostics",
            BenchmarkCase(Milliseconds(4.78), Milliseconds(5.39), MeasurementsInMilliseconds(4.78, 4.78, 4.78, 4.78, 4.78), Milliseconds(0.35)),
            BenchmarkCase(Milliseconds(5.66), Milliseconds(6.27), MeasurementsInMilliseconds(5.66, 5.66, 5.66, 5.66, 5.66), Milliseconds(0.36)));
        BdnComparisonResult moq1002 = CreateComparison(
            "Benchmark.Moq1002WithDiagnostics",
            BenchmarkCase(Milliseconds(3.20), Milliseconds(3.40), MeasurementsInMilliseconds(3.20, 3.20, 3.20, 3.20, 3.20), Milliseconds(0.226)),
            BenchmarkCase(Milliseconds(3.73), Milliseconds(3.93), MeasurementsInMilliseconds(3.73, 3.73, 3.73, 3.73, 3.73), Milliseconds(0.226)));

        bool hasRegression = strategy.HasRegression([moq1000, moq1002], new PerfDiffTestLogger(), out _);

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

    [Theory]
    [MemberData(nameof(StandardDeviationNoiseFloorCases))]
    public void ExceedsRatioNoiseFloor_WithStandardDeviationCases_ReturnsExpected(NoiseFloorCase testCase)
    {
        RegressionResult result = CreateRegressionResult(
            testCase.BaseMeanNs,
            testCase.DiffMeanNs,
            new MeasurementNoiseFixture
            {
                BaseStandardDeviationNs = testCase.BaseStandardDeviationNs,
                DiffStandardDeviationNs = testCase.DiffStandardDeviationNs,
                N = testCase.N,
            });

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => testCase.DeltaNs);

        Assert.Equal(testCase.Expected, exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenStandardDeviationIsUnavailable_UsesAbsoluteFloor()
    {
        RegressionResult result = CreateRegressionResult(Milliseconds(200), Milliseconds(220));

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.True(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenStandardErrorIsAvailable_UsesDerivedStandardDeviation()
    {
        RegressionResult result = CreateRegressionResult(
            Milliseconds(200),
            Milliseconds(220),
            new MeasurementNoiseFixture
            {
                BaseStandardDeviationNs = double.NaN,
                DiffStandardDeviationNs = double.NaN,
                BaseStandardErrorNs = Milliseconds(4),
                DiffStandardErrorNs = Milliseconds(4),
                N = 4,
            });

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.False(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenConfidenceIntervalStandardErrorIsAvailable_UsesDerivedStandardDeviation()
    {
        RegressionResult result = CreateRegressionResult(
            Milliseconds(200),
            Milliseconds(220),
            new MeasurementNoiseFixture
            {
                BaseStandardDeviationNs = double.NaN,
                DiffStandardDeviationNs = double.NaN,
                BaseStandardErrorNs = double.NaN,
                DiffStandardErrorNs = double.NaN,
                ConfidenceIntervalN = 4,
                ConfidenceIntervalStandardErrorNs = Milliseconds(4),
            });

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
    public void ExceedsRatioNoiseFloor_WhenStatisticsAreMissing_FallsBackToAbsoluteFloor()
    {
        RegressionResult result = new(
            "Benchmark.MissingStats",
            new Benchmark(),
            new Benchmark(),
            ComparisonResult.Lesser);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.True(exceeds);
    }

    [Fact]
    public void ExceedsRatioNoiseFloor_WhenDiffStatisticsAreMissing_FallsBackToAbsoluteFloor()
    {
        RegressionResult result = new(
            "Benchmark.MissingDiffStats",
            CreateBenchmark(
            "Benchmark.MissingDiffStats",
            new BenchmarkFixture
            {
                MeanNs = Milliseconds(200),
                P95Ns = Milliseconds(220),
                StandardDeviationNs = Milliseconds(1),
                N = 2,
            }),
            new Benchmark(),
            ComparisonResult.Lesser);

        bool exceeds = RegressionStrategyHelper.ExceedsRatioNoiseFloor(result, _ => Milliseconds(20));

        Assert.True(exceeds);
    }

    [Fact]
    public void IsAggregateRatioRegression_WhenGeomeanEqualsFivePercent_ReturnsFalse()
    {
        RegressionResult[] results = [CreateRegressionResult(Milliseconds(200), Milliseconds(210))];

        bool hasRegression = RegressionStrategyHelper.IsAggregateRatioRegression(results, _ => RegressionStrategyHelper.AggregateRatioRegressionThreshold, out double geomean);

        Assert.False(hasRegression);
        Assert.Equal(RegressionStrategyHelper.AggregateRatioRegressionThreshold, geomean);
    }

    [Fact]
    public void IsAggregateRatioRegression_WhenGeomeanExceedsFivePercent_ReturnsTrue()
    {
        RegressionResult[] results = [CreateRegressionResult(Milliseconds(200), Milliseconds(220))];

        bool hasRegression = RegressionStrategyHelper.IsAggregateRatioRegression(results, _ => TenPercentRatio, out double geomean);

        Assert.True(hasRegression);
        Assert.Equal(TenPercentRatio, geomean);
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

    private static RegressionResult CreateRegressionResult(double baseMeanNs, double diffMeanNs, MeasurementNoiseFixture? noise = null)
        => new(
            "Benchmark.A",
            CreateBenchmark("Benchmark.A", CreateBaselineFixture(baseMeanNs, noise)),
            CreateBenchmark("Benchmark.A", CreateDiffFixture(diffMeanNs, noise)),
            ComparisonResult.Lesser);

    private static BenchmarkFixture CreateBaselineFixture(double meanNs, MeasurementNoiseFixture? noise)
        => CreateNoiseFixture(
            meanNs,
            noise?.BaseStandardDeviationNs ?? 0,
            noise?.BaseStandardErrorNs ?? 0,
            noise);

    private static BenchmarkFixture CreateDiffFixture(double meanNs, MeasurementNoiseFixture? noise)
        => CreateNoiseFixture(
            meanNs,
            noise?.DiffStandardDeviationNs ?? 0,
            noise?.DiffStandardErrorNs ?? 0,
            noise);

    private static BenchmarkFixture CreateNoiseFixture(
        double meanNs,
        double standardDeviationNs,
        double standardErrorNs,
        MeasurementNoiseFixture? noise)
        => new()
        {
            MeanNs = meanNs,
            P95Ns = meanNs,
            StandardDeviationNs = standardDeviationNs,
            StandardErrorNs = standardErrorNs,
            N = noise?.N ?? 0,
            ConfidenceIntervalN = noise?.ConfidenceIntervalN ?? 0,
            ConfidenceIntervalStandardErrorNs = noise?.ConfidenceIntervalStandardErrorNs ?? 0,
        };

    private static BdnComparisonResult CreateComparison(string id, BenchmarkFixture baseline, BenchmarkFixture diff)
        => new(
            id,
            CreateBenchmark(id, baseline),
            CreateBenchmark(id, diff));

    private static Benchmark CreateBenchmark(string id, BenchmarkFixture fixture)
        => new()
        {
            FullName = id,
            Statistics = new Statistics
            {
                N = fixture.N,
                Median = fixture.MeanNs,
                Mean = fixture.MeanNs,
                StandardDeviation = fixture.StandardDeviationNs,
                StandardError = fixture.StandardErrorNs,
                ConfidenceInterval = fixture.ConfidenceIntervalN > 0
                    ? new global::PerfDiff.BDN.DataContracts.ConfidenceInterval
                    {
                        N = fixture.ConfidenceIntervalN,
                        StandardError = fixture.ConfidenceIntervalStandardErrorNs,
                    }
                    : null,
                Percentiles = new Percentiles
                {
                    P95 = fixture.P95Ns,
                },
            },
            Measurements = fixture.Measurements ?? MeasurementsInMilliseconds(fixture.MeanNs / TimeUnitConstants.NanoSecondsToMilliseconds),
        };

    private static BenchmarkFixture BenchmarkCase(double meanNs, double p95Ns, Measurement[] measurements, double standardDeviationNs = 0)
        => new()
        {
            MeanNs = meanNs,
            P95Ns = p95Ns,
            StandardDeviationNs = standardDeviationNs,
            N = measurements.Length,
            Measurements = measurements,
        };

    private static Measurement[] MeasurementsInMilliseconds(params double[] values)
        => values.Select(value => new Measurement
        {
            IterationStage = "Result",
            Operations = 1,
            Nanoseconds = Milliseconds(value),
        }).ToArray();

    private static Measurement[] MeasurementsInMicroseconds(params double[] values)
        => values.Select(value => new Measurement
        {
            IterationStage = "Result",
            Operations = 1,
            Nanoseconds = Microseconds(value),
        }).ToArray();

    private static double Milliseconds(double value) => value * TimeUnitConstants.NanoSecondsToMilliseconds;

    private static double Microseconds(double value) => value * MicrosecondsToNanoseconds;

    private sealed class BenchmarkFixture
    {
        public double MeanNs { get; init; }

        public double P95Ns { get; init; }

        public double StandardDeviationNs { get; init; }

        public double StandardErrorNs { get; init; }

        public int N { get; init; }

        public int ConfidenceIntervalN { get; init; }

        public double ConfidenceIntervalStandardErrorNs { get; init; }

        public Measurement[]? Measurements { get; init; }
    }

    private sealed class MeasurementNoiseFixture
    {
        public double BaseStandardDeviationNs { get; init; }

        public double DiffStandardDeviationNs { get; init; }

        public double BaseStandardErrorNs { get; init; }

        public double DiffStandardErrorNs { get; init; }

        public int N { get; init; }

        public int ConfidenceIntervalN { get; init; }

        public double ConfidenceIntervalStandardErrorNs { get; init; }
    }
}
