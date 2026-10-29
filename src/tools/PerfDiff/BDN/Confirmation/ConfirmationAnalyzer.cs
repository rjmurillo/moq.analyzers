using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Distributions.ContinuousDistributions;
using Pragmastat;

namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// Decides whether paired confirmation rounds reproduce the screen-stage regressions.
/// </summary>
/// <remarks>
/// <para>
/// One benchmark process measures one machine state, so its iterations cannot separate a
/// slower runner from slower code. Confirmation rounds run the baseline and current builds as
/// separate processes, in alternating order, and compare one log ratio per round.
/// </para>
/// <para>
/// The stop rule copies the BenchmarkDotNet rule for iterations: stop when the confidence
/// interval margin drops below a relative error target, within a minimum and maximum count.
/// The rule reads only the spread of the ratios, never their direction, so it does not bias
/// the test.
/// </para>
/// <para>
/// The decision is a one-sided paired t-test of the mean log ratio against the gate's minimum
/// effect. Holm's method keeps the chance of any false confirmation in one run at or below
/// <see cref="FamilyWiseErrorRate"/>.
/// </para>
/// </remarks>
internal static class ConfirmationAnalyzer
{
    /// <summary>
    /// The highest chance that any flagged benchmark is confirmed when none truly regressed.
    /// </summary>
    internal const double FamilyWiseErrorRate = 0.01;

    /// <summary>
    /// The confidence level of the interval that the stop rule reads.
    /// </summary>
    internal const double ConfidenceLevel = 0.99;

    /// <summary>
    /// The stop rule's relative error target. It matches the BenchmarkDotNet default.
    /// </summary>
    internal const double MaxRelativeError = 0.02;

    /// <summary>
    /// The fewest rounds a paired t-test can use.
    /// </summary>
    internal const int MinimumRounds = 2;

    private const double NanosecondsPerMillisecond = 1_000_000D;

    /// <summary>
    /// Analyzes the rounds that ran so far.
    /// </summary>
    /// <param name="hypotheses">The flagged benchmarks to confirm.</param>
    /// <param name="rounds">The paired rounds, in run order.</param>
    /// <param name="minRounds">The fewest rounds before the stop rule applies.</param>
    /// <param name="maxRounds">The round count that forces a decision.</param>
    /// <param name="logger">Receives one line per hypothesis.</param>
    /// <returns>The decision for this point in the run.</returns>
    internal static ConfirmationDecision Analyze(
        IReadOnlyList<ConfirmationHypothesis> hypotheses,
        IReadOnlyList<ConfirmationRound> rounds,
        int minRounds,
        int maxRounds,
        ILogger logger)
    {
        Debug.Assert(minRounds >= MinimumRounds, "A paired t-test needs at least two rounds.");
        Debug.Assert(maxRounds >= minRounds, "The maximum round count cannot be below the minimum.");

        if (hypotheses.Count == 0)
        {
            LogInformation(logger, "No flagged benchmarks to confirm.");
            return ConfirmationDecision.NotConfirmed;
        }

        List<HypothesisStatistics> statistics = new(hypotheses.Count);
        foreach (ConfirmationHypothesis hypothesis in hypotheses)
        {
            if (!TryGetStatistics(hypothesis, rounds, logger, out HypothesisStatistics? hypothesisStatistics))
            {
                return ConfirmationDecision.Error;
            }

            statistics.Add(hypothesisStatistics);
        }

        int roundCount = rounds.Count;
        if (roundCount < minRounds)
        {
            LogInformation(logger, $"Completed {roundCount} of at least {minRounds} confirmation rounds.");
            return ConfirmationDecision.Continue;
        }

        double maxLogMargin = Math.Log(1D + MaxRelativeError);
        if (roundCount < maxRounds && statistics.Exists(s => s.LogMargin > maxLogMargin))
        {
            LogInformation(logger, $"Completed {roundCount} confirmation rounds. A confidence margin is above {MaxRelativeError:P0}, so another round runs.");
            return ConfirmationDecision.Continue;
        }

        return Decide(statistics, roundCount, logger);
    }

    private static ConfirmationDecision Decide(List<HypothesisStatistics> statistics, int roundCount, ILogger logger)
    {
        bool[] rejected = HolmReject(statistics.Select(static s => s.PValue).ToArray(), FamilyWiseErrorRate);
        bool anyConfirmed = false;
        for (int index = 0; index < statistics.Count; index++)
        {
            HypothesisStatistics hypothesisStatistics = statistics[index];
            string? shortfall = GetShortfall(hypothesisStatistics);
            anyConfirmed |= rejected[index] && shortfall is null;
            LogResult(logger, hypothesisStatistics, roundCount, rejected[index], shortfall);
        }

        return anyConfirmed ? ConfirmationDecision.Confirmed : ConfirmationDecision.NotConfirmed;
    }

    /// <summary>
    /// Applies Holm's step-down method.
    /// </summary>
    /// <param name="pValues">One p-value per hypothesis.</param>
    /// <param name="alpha">The family-wise error rate.</param>
    /// <returns>For each hypothesis, whether its null hypothesis is rejected.</returns>
    internal static bool[] HolmReject(double[] pValues, double alpha)
    {
        int count = pValues.Length;
        int[] order = Enumerable.Range(0, count).OrderBy(index => pValues[index]).ToArray();
        bool[] rejected = new bool[count];
        for (int rank = 0; rank < count; rank++)
        {
            int index = order[rank];
            double pValue = pValues[index];
            if (double.IsNaN(pValue) || pValue > alpha / (count - rank))
            {
                break;
            }

            rejected[index] = true;
        }

        return rejected;
    }

    /// <summary>
    /// Computes the one-sided p-value and the confidence margin for a sample of log ratios.
    /// </summary>
    /// <param name="logRatios">One log ratio per round. Needs at least two values.</param>
    /// <param name="nullLogRatio">The log ratio that the test must exceed.</param>
    /// <returns>The mean, the p-value, and the margin, all on the log scale.</returns>
    internal static (double Mean, double PValue, double LogMargin) TestMeanGreater(double[] logRatios, double nullLogRatio)
    {
        int count = logRatios.Length;
        Debug.Assert(count >= MinimumRounds, "A t-test needs at least two values.");

        double mean = logRatios.Average();
        double sumOfSquares = logRatios.Sum(value => (value - mean) * (value - mean));
        double standardError = Math.Sqrt(sumOfSquares / (count - 1) / count);

        if (standardError <= 0D)
        {
            // Identical ratios leave no doubt about the direction.
            return (mean, mean > nullLogRatio ? 0D : 1D, 0D);
        }

        StudentDistribution distribution = new(count - 1);
        double t = (mean - nullLogRatio) / standardError;
        double pValue = 1D - distribution.Cdf(t);
        double quantile = distribution.Quantile(new Probability(1D - ((1D - ConfidenceLevel) / 2D)));
        return (mean, pValue, quantile * standardError);
    }

    private static bool TryGetStatistics(
        ConfirmationHypothesis hypothesis,
        IReadOnlyList<ConfirmationRound> rounds,
        ILogger logger,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out HypothesisStatistics? statistics)
    {
        statistics = null;
        double[] logRatios = new double[rounds.Count];
        double[] currentValues = new double[rounds.Count];
        double[] floorDeltas = new double[rounds.Count];
        for (int index = 0; index < rounds.Count; index++)
        {
            if (!TryGetRoundValues(hypothesis, rounds[index], out logRatios[index], out currentValues[index], out floorDeltas[index]))
            {
                LogError(logger, $"Confirmation round {index + 1} has no positive {hypothesis.Metric} or noise floor metric for '{hypothesis.BenchmarkId}'.");
                return false;
            }
        }

        double medianFloorDeltaNs = hypothesis.Floor is null ? double.NaN : Median(floorDeltas);
        if (rounds.Count < MinimumRounds)
        {
            statistics = new HypothesisStatistics(hypothesis, double.NaN, PValue: 1D, LogMargin: double.PositiveInfinity, double.NaN, medianFloorDeltaNs);
            return true;
        }

        (double mean, double pValue, double logMargin) = TestMeanGreater(logRatios, Math.Log(1D + hypothesis.MinimumEffect));
        statistics = new HypothesisStatistics(hypothesis, mean, pValue, logMargin, Median(currentValues), medianFloorDeltaNs);
        return true;
    }

    private static bool TryGetRoundValues(
        ConfirmationHypothesis hypothesis,
        ConfirmationRound round,
        out double logRatio,
        out double currentNs,
        out double floorDeltaNs)
    {
        logRatio = double.NaN;
        floorDeltaNs = double.NaN;
        if (!TryGetMetric(hypothesis.Metric, hypothesis.BenchmarkId, round.Baseline, out double baselineNs)
            || !TryGetMetric(hypothesis.Metric, hypothesis.BenchmarkId, round.Current, out currentNs))
        {
            currentNs = double.NaN;
            return false;
        }

        logRatio = Math.Log(currentNs / baselineNs);
        if (hypothesis.Floor is not DeltaFloor floor)
        {
            return true;
        }

        if (!TryGetMetric(floor.Metric, hypothesis.BenchmarkId, round.Baseline, out double floorBaselineNs)
            || !TryGetMetric(floor.Metric, hypothesis.BenchmarkId, round.Current, out double floorCurrentNs))
        {
            return false;
        }

        floorDeltaNs = floorCurrentNs - floorBaselineNs;
        return true;
    }

    private static bool TryGetMetric(ConfirmationMetric metric, string benchmarkId, IReadOnlyDictionary<string, Benchmark> benchmarks, out double valueNs)
    {
        valueNs = double.NaN;
        if (!benchmarks.TryGetValue(benchmarkId, out Benchmark? benchmark)
            || ConfirmationHypothesis.Select(metric, benchmark) is not double value
            || !IsPositiveFinite(value))
        {
            return false;
        }

        valueNs = value;
        return true;
    }

    private static bool IsPositiveFinite(double value) => double.IsFinite(value) && value > 0D;

    /// <summary>
    /// Finds the screen condition, other than the ratio test, that the rounds do not meet.
    /// </summary>
    /// <param name="statistics">The hypothesis statistics.</param>
    /// <returns>The reason the regression does not count, or <see langword="null"/> when it does.</returns>
    private static string? GetShortfall(HypothesisStatistics statistics)
    {
        ConfirmationHypothesis hypothesis = statistics.Hypothesis;
        if (hypothesis.BudgetNs is double budgetNs && statistics.CurrentMedianNs <= budgetNs)
        {
            return "current build is back under budget";
        }

        if (hypothesis.Floor is DeltaFloor floor && statistics.MedianFloorDeltaNs <= floor.MinimumNs)
        {
            return $"median paired {floor.Metric} slowdown of {statistics.MedianFloorDeltaNs / NanosecondsPerMillisecond:F3} ms is not above the {floor.MinimumNs / NanosecondsPerMillisecond:F3} ms noise floor";
        }

        return null;
    }

    private static double Median(double[] values)
    {
        double[] sorted = values.Order().ToArray();
        int middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2D;
    }

    private static void LogResult(ILogger logger, HypothesisStatistics statistics, int roundCount, bool significant, string? shortfall)
    {
        ConfirmationHypothesis hypothesis = statistics.Hypothesis;
        bool confirmed = significant && shortfall is null;
        string verdict = (significant, shortfall) switch
        {
            (false, _) => "not confirmed: slowdown is not significant",
            (true, null) => "CONFIRMED regression",
            (true, string reason) => $"not confirmed: {reason}",
        };

        string budget = string.Empty;
        if (hypothesis.BudgetNs is double budgetNs)
        {
            string side = statistics.CurrentMedianNs > budgetNs ? "over" : "under";
            budget = $", current median {statistics.CurrentMedianNs / NanosecondsPerMillisecond:F3} ms vs budget {budgetNs / NanosecondsPerMillisecond:F3} ms ({side})";
        }

        string message =
            $"{hypothesis.BenchmarkId} {hypothesis.Metric}: {roundCount} rounds, ratio {Math.Exp(statistics.Mean):F3}, "
            + $"{ConfidenceLevel:P0} CI [{Math.Exp(statistics.Mean - statistics.LogMargin):F3}; {Math.Exp(statistics.Mean + statistics.LogMargin):F3}], "
            + $"must exceed {1D + hypothesis.MinimumEffect:F3}, p = {statistics.PValue:G3}{budget}. {verdict}.";

        if (confirmed)
        {
            LogError(logger, message);
        }
        else
        {
            LogInformation(logger, message);
        }
    }

#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates
#pragma warning disable CA2254 // The logging message template should not vary between calls
    private static void LogInformation(ILogger logger, string message) => logger.LogInformation(message);

    private static void LogError(ILogger logger, string message) => logger.LogError(message);
#pragma warning restore CA2254
#pragma warning restore CA1848

    private sealed record HypothesisStatistics(
        ConfirmationHypothesis Hypothesis,
        double Mean,
        double PValue,
        double LogMargin,
        double CurrentMedianNs,
        double MedianFloorDeltaNs);
}
