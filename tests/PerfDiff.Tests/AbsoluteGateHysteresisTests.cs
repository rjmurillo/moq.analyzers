using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;
using Xunit;

namespace PerfDiff.Tests;

/// <summary>
/// Covers the 5% hysteresis margin on the absolute P95 (250 ms) and mean (100 ms) gates.
/// </summary>
public sealed class AbsoluteGateHysteresisTests
{
    private static readonly double ClearlyOverLimitFactor = 1.1D;
    private static readonly double WellOverLimitFactor = 1.2D;
    private static readonly int ImprovedBenchmarkCount = 2;
    private static readonly double P95ThresholdMs = 250D;
    private static readonly double MeanThresholdMs = 100D;
    private static readonly string BetterLogFragment = "at or under the threshold";
    private static readonly string WorseLogFragment = "over the effective limit";

    public static TheoryData<bool> GateKinds() => new() { true, false };

    [Fact]
    public void AbsoluteGateHysteresisMultiplier_IsFivePercent()
    {
        const double expectedMultiplier = 1.05D;
        Assert.Equal(expectedMultiplier, RegressionStrategyHelper.AbsoluteGateHysteresisMultiplier);
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenCurrentIsExactlyAtEffectiveLimit_ReturnsFalse(bool useP95)
    {
        Gate gate = Gate.For(useP95);

        bool hasRegression = gate.HasRegression(gate.ThresholdNs, gate.EffectiveLimitNs, new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Empty(details.RegressedIds);
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenCurrentIsJustAboveEffectiveLimit_ReturnsTrue(bool useP95)
    {
        Gate gate = Gate.For(useP95);

        bool hasRegression = gate.HasRegression(gate.ThresholdNs, Math.BitIncrement(gate.EffectiveLimitNs), new PerfDiffTestLogger(), out RegressionDetectionResult details);

        Assert.True(hasRegression);
        Assert.Equal(["Benchmark.A"], details.RegressedIds);
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenCurrentIsJustBelowEffectiveLimit_ReturnsFalse(bool useP95)
    {
        Gate gate = Gate.For(useP95);

        bool hasRegression = gate.HasRegression(gate.ThresholdNs, Math.BitDecrement(gate.EffectiveLimitNs), new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenCurrentCrossesThresholdByLessThanMargin_ReturnsFalse(bool useP95)
    {
        // The nightly failure: baseline under 250 ms, current 250.055 ms.
        Gate gate = Gate.For(useP95);
        double baselineNs = gate.ThresholdNs * 0.99D;
        double currentNs = gate.ThresholdNs + 55_000D;

        bool hasRegression = gate.HasRegression(baselineNs, currentNs, new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenBaselineIsJustAboveThreshold_ReturnsFalse(bool useP95)
    {
        Gate gate = Gate.For(useP95);

        bool hasRegression = gate.HasRegression(Math.BitIncrement(gate.ThresholdNs), gate.EffectiveLimitNs * 2D, new PerfDiffTestLogger(), out _);

        Assert.False(hasRegression);
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenBaselineIsExactlyAtThreshold_CanRegress(bool useP95)
    {
        Gate gate = Gate.For(useP95);

        bool hasRegression = gate.HasRegression(gate.ThresholdNs, gate.EffectiveLimitNs * ClearlyOverLimitFactor, new PerfDiffTestLogger(), out _);

        Assert.True(hasRegression);
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenWorse_LogsEffectiveLimit(bool useP95)
    {
        Gate gate = Gate.For(useP95);
        PerfDiffTestLogger logger = new();

        gate.HasRegression(gate.ThresholdNs, gate.EffectiveLimitNs * ClearlyOverLimitFactor, logger, out _);

        string expectedLimit = (gate.EffectiveLimitNs / TimeUnitConstants.NanoSecondsToMilliseconds).ToString("F3", System.Globalization.CultureInfo.CurrentCulture);
        Assert.Contains(logger.Messages, message => message.Contains(WorseLogFragment, StringComparison.Ordinal) && message.Contains(expectedLimit, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenBaselineIsExactlyAtEffectiveLimitAndCurrentIsUnderThreshold_DoesNotCountAsBetter(bool useP95)
    {
        Gate gate = Gate.For(useP95);
        PerfDiffTestLogger logger = new();

        bool hasRegression = gate.HasRegression(gate.EffectiveLimitNs, gate.ThresholdNs * 0.5D, logger, out _);

        Assert.False(hasRegression);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(BetterLogFragment, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenBaselineIsJustAboveEffectiveLimitAndCurrentIsUnderThreshold_CountsAsBetter(bool useP95)
    {
        Gate gate = Gate.For(useP95);
        PerfDiffTestLogger logger = new();

        bool hasRegression = gate.HasRegression(Math.BitIncrement(gate.EffectiveLimitNs), gate.ThresholdNs, logger, out RegressionDetectionResult details);

        Assert.False(hasRegression);
        Assert.Empty(details.RegressedIds);
        Assert.Contains(logger.Messages, message => message.Contains(BetterLogFragment, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(GateKinds))]
    public void HasRegression_WhenBaselineIsAboveEffectiveLimitAndCurrentIsJustOverThreshold_DoesNotCountAsBetter(bool useP95)
    {
        Gate gate = Gate.For(useP95);
        PerfDiffTestLogger logger = new();

        gate.HasRegression(gate.EffectiveLimitNs * WellOverLimitFactor, Math.BitIncrement(gate.ThresholdNs), logger, out _);

        Assert.DoesNotContain(logger.Messages, message => message.Contains(BetterLogFragment, StringComparison.Ordinal));
    }

    [Fact]
    public void HasRegression_WhenSeveralBenchmarksImprove_LogsThemInIdOrderWithoutRegression()
    {
        PercentileRegressionStrategy strategy = new();
        double thresholdNs = P95ThresholdMs * TimeUnitConstants.NanoSecondsToMilliseconds;
        double overNs = thresholdNs * WellOverLimitFactor;
        BdnComparisonResult[] comparison =
        [
            new("Zeta", BenchmarkTestData.CreateBenchmark("Zeta", p95Ns: overNs), BenchmarkTestData.CreateBenchmark("Zeta", p95Ns: thresholdNs)),
            new("Alpha", BenchmarkTestData.CreateBenchmark("Alpha", p95Ns: overNs), BenchmarkTestData.CreateBenchmark("Alpha", p95Ns: thresholdNs)),
        ];
        PerfDiffTestLogger logger = new();

        bool hasRegression = strategy.HasRegression(comparison, logger, out _);

        Assert.False(hasRegression);
        string[] betterMessages = logger.Messages.Where(message => message.Contains(BetterLogFragment, StringComparison.Ordinal)).ToArray();
        Assert.Equal(ImprovedBenchmarkCount, betterMessages.Length);
        Assert.Contains("'Alpha'", betterMessages[0], StringComparison.Ordinal);
        Assert.Contains("'Zeta'", betterMessages[1], StringComparison.Ordinal);
    }

    private sealed class Gate
    {
        private readonly bool _useP95;

        private Gate(bool useP95, double thresholdMs)
        {
            _useP95 = useP95;
            ThresholdNs = thresholdMs * TimeUnitConstants.NanoSecondsToMilliseconds;
        }

        public double ThresholdNs { get; }

        public double EffectiveLimitNs => ThresholdNs * RegressionStrategyHelper.AbsoluteGateHysteresisMultiplier;

        public static Gate For(bool useP95) => new(useP95, useP95 ? P95ThresholdMs : MeanThresholdMs);

        public bool HasRegression(double baselineNs, double currentNs, PerfDiffTestLogger logger, out RegressionDetectionResult details)
        {
            IBenchmarkRegressionStrategy strategy = _useP95 ? new PercentileRegressionStrategy() : new MeanWallClockRegressionStrategy();
            BdnComparisonResult comparison = new(
                "Benchmark.A",
                BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: _useP95 ? 1 : baselineNs, p95Ns: _useP95 ? baselineNs : 1),
                BenchmarkTestData.CreateBenchmark("Benchmark.A", meanNs: _useP95 ? 1 : currentNs, p95Ns: _useP95 ? currentNs : 1));
            return strategy.HasRegression([comparison], logger, out details);
        }
    }
}
