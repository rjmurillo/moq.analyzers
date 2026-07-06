using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Xunit;

namespace PerfDiff.Tests;

public sealed class AbsoluteBudgetRegressionStrategyTests
{
    [Fact]
    public void PercentileRegressionStrategy_WhenBenchmarkRemainsOverBudget_ReturnsFalse()
    {
        PercentileRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression(
            [CreateComparison(baseP95Ns: Milliseconds(300), diffP95Ns: Milliseconds(310))],
            new PerfDiffTestLogger(),
            out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenBenchmarkCrossesOverBudget_ReturnsTrue()
    {
        PercentileRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression(
            [CreateComparison(baseP95Ns: Milliseconds(100), diffP95Ns: Milliseconds(260))],
            new PerfDiffTestLogger(),
            out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenBenchmarkEqualsBudget_ReturnsFalse()
    {
        PercentileRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression(
            [CreateComparison(baseP95Ns: Milliseconds(100), diffP95Ns: Milliseconds(250))],
            new PerfDiffTestLogger(),
            out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void PercentileRegressionStrategy_WhenBenchmarkCrossesUnderBudget_ReturnsFalse()
    {
        PercentileRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression(
            [CreateComparison(baseP95Ns: Milliseconds(300), diffP95Ns: Milliseconds(200))],
            new PerfDiffTestLogger(),
            out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void MeanWallClockRegressionStrategy_WhenBenchmarkRemainsOverBudget_ReturnsFalse()
    {
        MeanWallClockRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression(
            [CreateComparison(baseMeanNs: Milliseconds(150), diffMeanNs: Milliseconds(160))],
            new PerfDiffTestLogger(),
            out _);

        Assert.False(hasRegression);
    }

    [Fact]
    public void MeanWallClockRegressionStrategy_WhenBenchmarkCrossesOverBudget_ReturnsTrue()
    {
        MeanWallClockRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression(
            [CreateComparison(baseMeanNs: Milliseconds(50), diffMeanNs: Milliseconds(120))],
            new PerfDiffTestLogger(),
            out _);

        Assert.True(hasRegression);
    }

    [Fact]
    public void MeanWallClockRegressionStrategy_WhenBenchmarkCrossesUnderBudget_ReturnsFalse()
    {
        MeanWallClockRegressionStrategy strategy = new();

        bool hasRegression = strategy.HasRegression(
            [CreateComparison(baseMeanNs: Milliseconds(150), diffMeanNs: Milliseconds(80))],
            new PerfDiffTestLogger(),
            out _);

        Assert.False(hasRegression);
    }

    private static BdnComparisonResult CreateComparison(
        double baseMeanNs = 100,
        double diffMeanNs = 100,
        double baseP95Ns = 100,
        double diffP95Ns = 100)
        => new(
            "Benchmark.A",
            BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: baseMeanNs, p95Ns: baseP95Ns),
            BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: diffMeanNs, p95Ns: diffP95Ns));

    private static double Milliseconds(double value) => value * TimeUnitConstants.NanoSecondsToMilliseconds;
}
