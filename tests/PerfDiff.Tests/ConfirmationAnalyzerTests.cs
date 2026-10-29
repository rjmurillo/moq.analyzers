using PerfDiff.BDN.Confirmation;
using PerfDiff.BDN.DataContracts;
using Xunit;

namespace PerfDiff.Tests;

public sealed class ConfirmationAnalyzerTests
{
    private static readonly string BenchmarkId = "Ns.Bench.Method(FileCount: 1000)";
    private static readonly double BaselineNs = 200_000_000D;
    private static readonly double NoiseFloorNs = 500_000D;

    [Fact]
    public void Analyze_WithNoHypotheses_ReturnsNotConfirmed()
    {
        ConfirmationDecision decision = Analyze([], CreateRounds(1.0, 1.0, 1.0));

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
    }

    [Fact]
    public void Analyze_BeforeMinRounds_ReturnsContinue()
    {
        ConfirmationDecision decision = Analyze([RatioHypothesis()], CreateRounds(1.20, 1.20));

        Assert.Equal(ConfirmationDecision.Continue, decision);
    }

    [Fact]
    public void Analyze_WithNoRounds_ReturnsContinue()
    {
        ConfirmationDecision decision = Analyze([RatioHypothesis()], []);

        Assert.Equal(ConfirmationDecision.Continue, decision);
    }

    [Fact]
    public void Analyze_WhenMarginIsAboveTargetBeforeMaxRounds_ReturnsContinue()
    {
        ConfirmationDecision decision = Analyze([RatioHypothesis()], CreateRounds(0.90, 1.15, 1.02));

        Assert.Equal(ConfirmationDecision.Continue, decision);
    }

    [Fact]
    public void Analyze_WhenNoisyAtMaxRounds_DecidesNotConfirmed()
    {
        ConfirmationDecision decision = Analyze([RatioHypothesis()], CreateRounds(0.90, 1.15, 1.02), maxRounds: 3);

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
    }

    [Fact]
    public void Analyze_WhenSlowdownIsConsistentAndAboveMinimumEffect_ReturnsConfirmed()
    {
        PerfDiffTestLogger logger = new();

        ConfirmationDecision decision = Analyze([RatioHypothesis()], CreateRounds(1.100, 1.105, 1.098, 1.102), logger: logger);

        Assert.Equal(ConfirmationDecision.Confirmed, decision);
        Assert.Contains(logger.Messages, message => message.Contains("CONFIRMED regression", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WhenRatioAndDeltaClearTheNoiseFloor_ReturnsConfirmed()
    {
        ConfirmationDecision decision = Analyze([FloorHypothesis(ConfirmationMetric.Mean)], CreateRounds(1.100, 1.105, 1.098, 1.102));

        Assert.Equal(ConfirmationDecision.Confirmed, decision);
    }

    [Fact]
    public void Analyze_WhenRatioPersistsButDeltaIsUnderTheNoiseFloor_ReturnsNotConfirmed()
    {
        PerfDiffTestLogger logger = new();

        // 4 ms to 4.3 ms is a 7.5% slowdown but only 0.3 ms, under the 0.5 ms screen floor.
        ConfirmationDecision decision = Analyze(
            [FloorHypothesis(ConfirmationMetric.Mean)],
            CreateRoundsWithBaseline(4_000_000D, 1.075, 1.074, 1.076, 1.075),
            logger: logger);

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
        Assert.Contains(logger.Messages, message => message.Contains("0.500 ms noise floor", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WhenOnlyTheP95TailRegresses_ReturnsNotConfirmed()
    {
        PerfDiffTestLogger logger = new();
        ConfirmationHypothesis hypothesis = new(BenchmarkId, ConfirmationMetric.P95, 0.05, BudgetNs: null, new DeltaFloor(ConfirmationMetric.Mean, NoiseFloorNs));
        ConfirmationRound[] rounds = [.. new[] { 1.100, 1.105, 1.098, 1.102 }.Select(ratio => new ConfirmationRound(
            Map(BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: BaselineNs, meanNs: BaselineNs, p95Ns: BaselineNs)),
            Map(BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: BaselineNs, meanNs: BaselineNs, p95Ns: BaselineNs * ratio))))];

        ConfirmationDecision decision = Analyze([hypothesis], rounds, logger: logger);

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
        Assert.Contains(logger.Messages, message => message.Contains("median paired Mean slowdown of 0.000 ms", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WhenNoiseFloorMetricIsMissing_ReturnsError()
    {
        ConfirmationHypothesis hypothesis = new(BenchmarkId, ConfirmationMetric.P95, 0.05, BudgetNs: null, new DeltaFloor(ConfirmationMetric.Mean, NoiseFloorNs));
        ConfirmationRound[] rounds = [.. Enumerable.Range(0, 3).Select(_ => new ConfirmationRound(
            Map(BenchmarkTestData.CreateBenchmark(BenchmarkId, meanNs: 0D, p95Ns: BaselineNs)),
            Map(BenchmarkTestData.CreateBenchmark(BenchmarkId, meanNs: 0D, p95Ns: BaselineNs * 1.1))))];

        Assert.Equal(ConfirmationDecision.Error, Analyze([hypothesis], rounds));
    }

    [Fact]
    public void Analyze_WithNoiseFloorBeforeMinRounds_ReturnsContinue()
    {
        Assert.Equal(ConfirmationDecision.Continue, Analyze([FloorHypothesis(ConfirmationMetric.Mean)], CreateRounds(1.1)));
    }

    [Fact]
    public void Analyze_WhenSlowdownIsRealButBelowMinimumEffect_ReturnsNotConfirmed()
    {
        PerfDiffTestLogger logger = new();

        ConfirmationDecision decision = Analyze([RatioHypothesis()], CreateRounds(1.030, 1.031, 1.029, 1.030), logger: logger);

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
        Assert.Contains(logger.Messages, message => message.Contains("slowdown is not significant", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WhenScreenShiftCameFromRunnerDrift_ReturnsNotConfirmed()
    {
        // The screen saw +8.8% from one run per side. Paired rounds show no code effect.
        ConfirmationDecision decision = Analyze(
            [BudgetHypothesis(budgetMs: 250)],
            CreateRounds(1.004, 0.997, 1.002, 0.999, 1.001, 0.998));

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
    }

    [Fact]
    public void Analyze_WhenBudgetSlowdownIsSignificantAndOverBudget_ReturnsConfirmed()
    {
        ConfirmationDecision decision = Analyze(
            [BudgetHypothesis(budgetMs: 250)],
            CreateRoundsWithBaseline(240_000_000D, 1.060, 1.061, 1.059, 1.060));

        Assert.Equal(ConfirmationDecision.Confirmed, decision);
    }

    [Fact]
    public void Analyze_WhenBudgetSlowdownIsSignificantButUnderBudget_ReturnsNotConfirmed()
    {
        PerfDiffTestLogger logger = new();

        ConfirmationDecision decision = Analyze(
            [BudgetHypothesis(budgetMs: 250)],
            CreateRoundsWithBaseline(220_000_000D, 1.060, 1.061, 1.059, 1.060),
            logger: logger);

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
        Assert.Contains(logger.Messages, message => message.Contains("back under budget", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("(under)", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WithTwoHypotheses_ConfirmsOnlyTheRegressedOne()
    {
        const string otherId = "Ns.Bench.Other";
        ConfirmationHypothesis regressed = RatioHypothesis();
        ConfirmationHypothesis stable = new(otherId, ConfirmationMetric.Mean, 0.05, BudgetNs: null);
        double[] regressedRatios = [1.100, 1.105, 1.098, 1.102];
        double[] stableRatios = [1.001, 0.999, 1.000, 1.000];
        ConfirmationRound[] rounds = regressedRatios
            .Select((ratio, index) => new ConfirmationRound(
                Map(Bench(BenchmarkId, BaselineNs), Bench(otherId, BaselineNs)),
                Map(Bench(BenchmarkId, BaselineNs * ratio), Bench(otherId, BaselineNs * stableRatios[index]))))
            .ToArray();
        PerfDiffTestLogger logger = new();

        ConfirmationDecision decision = Analyze([regressed, stable], rounds, logger: logger);

        Assert.Equal(ConfirmationDecision.Confirmed, decision);
        Assert.Contains(logger.Messages, message => message.StartsWith(BenchmarkId, StringComparison.Ordinal) && message.Contains("CONFIRMED", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.StartsWith(otherId, StringComparison.Ordinal) && message.Contains("not confirmed", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WhenBenchmarkIsMissingFromRound_ReturnsError()
    {
        ConfirmationRound round = new(Map(Bench(BenchmarkId, BaselineNs)), Map(Bench("Ns.Bench.Different", BaselineNs)));
        PerfDiffTestLogger logger = new();

        ConfirmationDecision decision = Analyze([RatioHypothesis()], [round, round, round], logger: logger);

        Assert.Equal(ConfirmationDecision.Error, decision);
        Assert.Contains(logger.Messages, message => message.Contains("round 1", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WhenMetricIsNotPositiveAndFinite_ReturnsError()
    {
        double[] invalidValues = [0D, -1D, double.NaN, double.PositiveInfinity];
        foreach (double baselineNs in invalidValues)
        {
            ConfirmationRound round = new(Map(Bench(BenchmarkId, baselineNs)), Map(Bench(BenchmarkId, BaselineNs)));

            ConfirmationDecision decision = Analyze([RatioHypothesis()], [round, round, round]);

            Assert.Equal(ConfirmationDecision.Error, decision);
        }
    }

    [Fact]
    public void Analyze_WhenMetricHasNoStatistics_ReturnsError()
    {
        ConfirmationRound round = new(Map(new Benchmark { FullName = BenchmarkId }), Map(Bench(BenchmarkId, BaselineNs)));

        ConfirmationDecision decision = Analyze([RatioHypothesis()], [round, round, round]);

        Assert.Equal(ConfirmationDecision.Error, decision);
    }

    [Fact]
    public void Analyze_WhenRatiosAreIdenticalAndAboveEffect_ReturnsConfirmed()
    {
        ConfirmationDecision decision = Analyze([RatioHypothesis()], CreateRounds(1.20, 1.20, 1.20));

        Assert.Equal(ConfirmationDecision.Confirmed, decision);
    }

    [Fact]
    public void Analyze_WhenRatiosAreIdenticalAndBelowEffect_ReturnsNotConfirmed()
    {
        ConfirmationDecision decision = Analyze([RatioHypothesis()], CreateRounds(1.0, 1.0, 1.0));

        Assert.Equal(ConfirmationDecision.NotConfirmed, decision);
    }

    [Fact]
    public void TestMeanGreater_MatchesTextbookOneSidedT()
    {
        // mean 0.2, s 0.1, n 3: t = 3.4641 with 2 degrees of freedom.
        (double mean, double pValue, double logMargin) = ConfirmationAnalyzer.TestMeanGreater([0.1, 0.2, 0.3], 0D);

        Assert.Equal(0.2, mean, 12);
        Assert.Equal(0.03709, pValue, 4);

        // t(0.995, 2) = 9.9248, times the standard error 0.057735.
        Assert.Equal(0.57301, logMargin, 4);
    }

    [Fact]
    public void HolmReject_StepsDownAndStopsAtFirstFailure()
    {
        bool[] rejected = ConfirmationAnalyzer.HolmReject([0.01, 0.04, 0.03, 0.005], 0.05);

        Assert.Equal(new[] { true, false, false, true }, rejected);
    }

    [Fact]
    public void HolmReject_WithNaN_DoesNotReject()
    {
        bool[] rejected = ConfirmationAnalyzer.HolmReject([double.NaN], 0.05);

        Assert.Equal(new[] { false }, rejected);
    }

    [Fact]
    public void HolmReject_WithNoPValues_ReturnsEmpty()
    {
        Assert.Empty(ConfirmationAnalyzer.HolmReject([], 0.05));
    }

    [Fact]
    public void SelectMetric_ReadsTheMatchingStatistic()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark(BenchmarkId, medianNs: 1, meanNs: 2, p95Ns: 3);

        Assert.Equal(1D, new ConfirmationHypothesis(BenchmarkId, ConfirmationMetric.Median, 0D, BudgetNs: null).SelectMetric(benchmark));
        Assert.Equal(2D, new ConfirmationHypothesis(BenchmarkId, ConfirmationMetric.Mean, 0D, BudgetNs: null).SelectMetric(benchmark));
        Assert.Equal(3D, new ConfirmationHypothesis(BenchmarkId, ConfirmationMetric.P95, 0D, BudgetNs: null).SelectMetric(benchmark));
    }

    [Fact]
    public void SelectMetric_WithoutStatistics_ReturnsNull()
    {
        Benchmark benchmark = new() { FullName = BenchmarkId };
        Benchmark withoutPercentiles = new() { FullName = BenchmarkId, Statistics = new Statistics() };

        foreach (ConfirmationMetric metric in Enum.GetValues<ConfirmationMetric>())
        {
            Assert.Null(new ConfirmationHypothesis(BenchmarkId, metric, 0D, BudgetNs: null).SelectMetric(benchmark));
        }

        Assert.Null(new ConfirmationHypothesis(BenchmarkId, ConfirmationMetric.P95, 0D, BudgetNs: null).SelectMetric(withoutPercentiles));
    }

    private static ConfirmationDecision Analyze(
        ConfirmationHypothesis[] hypotheses,
        ConfirmationRound[] rounds,
        int minRounds = 3,
        int maxRounds = 10,
        PerfDiffTestLogger? logger = null)
        => ConfirmationAnalyzer.Analyze(hypotheses, rounds, minRounds, maxRounds, logger ?? new PerfDiffTestLogger());

    private static ConfirmationHypothesis RatioHypothesis()
        => new(BenchmarkId, ConfirmationMetric.Mean, 0.05, BudgetNs: null);

    private static ConfirmationHypothesis FloorHypothesis(ConfirmationMetric floorMetric)
        => new(BenchmarkId, ConfirmationMetric.Mean, 0.05, BudgetNs: null, new DeltaFloor(floorMetric, NoiseFloorNs));

    private static ConfirmationHypothesis BudgetHypothesis(double budgetMs)
        => new(BenchmarkId, ConfirmationMetric.Mean, 0D, budgetMs * 1_000_000D);

    private static ConfirmationRound[] CreateRounds(params double[] ratios)
        => CreateRoundsWithBaseline(BaselineNs, ratios);

    private static ConfirmationRound[] CreateRoundsWithBaseline(double baselineNs, params double[] ratios)
        => ratios
            .Select(ratio => new ConfirmationRound(Map(Bench(BenchmarkId, baselineNs)), Map(Bench(BenchmarkId, baselineNs * ratio))))
            .ToArray();

    private static Benchmark Bench(string id, double valueNs)
        => BenchmarkTestData.CreateBenchmark(id, medianNs: valueNs, meanNs: valueNs, p95Ns: valueNs);

    private static Dictionary<string, Benchmark> Map(params Benchmark[] benchmarks)
        => benchmarks.ToDictionary(static benchmark => benchmark.FullName!, StringComparer.Ordinal);
}
