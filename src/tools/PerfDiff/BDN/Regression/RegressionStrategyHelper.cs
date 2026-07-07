using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Common;
using Perfolizer.Metrology;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Provides shared logic for regression strategies using <see cref="Threshold" />.
/// </summary>
public static class RegressionStrategyHelper
{
    private static readonly Func<RegressionResult, double> MedianRatioSelector = BenchmarkDotNetDiffer.GetMedianRatio;

    /// <summary>
    /// Relative ratio threshold for aggregate mean and P95 ratio gates.
    /// </summary>
    internal const double AggregateRatioRegressionThreshold = 1.05D;

    /// <summary>
    /// Display form of the 5% aggregate ratio threshold used by Perfolizer.
    /// </summary>
    internal const string AggregateRatioRegressionThresholdText = "5%";

    /// <summary>
    /// Lower bound for benchmark deltas that can affect ratio gates.
    /// </summary>
    internal const double AbsoluteNoiseFloorNs = 0.5D * TimeUnitConstants.NanoSecondsToMilliseconds;

    /// <summary>
    /// Multiplier for the combined standard deviation noise band.
    /// </summary>
    internal const double NoiseBandStandardDeviationMultiplier = 2D;

    public static bool HasRegression(
        BdnComparisonResult[] comparison,
        ILogger logger,
        RegressionMetricConfig config,
        out RegressionDetectionResult details)
    {
        RegressionResult[] notSame = FindRegressions(comparison, config.ThresholdValueNs, config.MetricSelector);
        List<RegressionResult> better = notSame.Where(result => result.Conclusion == ComparisonResult.Greater).OrderBy(k => k.Id).ToList();
        List<RegressionResult> worse = notSame.Where(result => result.Conclusion == ComparisonResult.Lesser).OrderBy(k => k.Id).ToList();
        int betterCount = better.Count;
        int worseCount = worse.Count;

        if (betterCount > 0)
        {
            foreach (RegressionResult betterResult in better)
            {
                double value = config.DisplayValueSelector(betterResult);
                logger.LogInformation("test: '{TestId}' {MetricName} took {MetricValue:F3} ms; better than the threshold {Threshold}", betterResult.Id, config.MetricName, value, config.DisplayThreshold);
            }

            double betterGeoMean = GetGeometricMeanOrNaN(better, MedianRatioSelector);
            if (!double.IsNaN(betterGeoMean))
            {
                logger.LogInformation("========== {MetricName} {BetterCount} better, geomean: {BetterGeoMean:F3}% ==========", config.MetricName, betterCount, ToPercentChange(betterGeoMean));
            }
        }

        if (worseCount > 0)
        {
            foreach (RegressionResult worseResult in worse)
            {
                double value = config.DisplayValueSelector(worseResult);
                logger.LogInformation("test: '{TestId}' {MetricName} took {MetricValue:F3} ms; worse than the threshold {Threshold}", worseResult.Id, config.MetricName, value, config.DisplayThreshold);
            }

            double worseGeoMean = GetGeometricMeanOrNaN(worse, MedianRatioSelector);
            if (!double.IsNaN(worseGeoMean))
            {
                logger.LogInformation("========== {MetricName} {WorseCount} worse, geomean: {WorseGeoMean:F3}% ==========", config.MetricName, worseCount, ToPercentChange(worseGeoMean));
            }
        }

        details = new RegressionDetectionResult(config.MetricName, config.DisplayThreshold);
        return worseCount > 0;
    }

    /// <summary>
    /// Detects ratio regressions by logging stable per-benchmark changes, then gating on the worse-set geomean.
    /// </summary>
    /// <param name="comparison">Benchmark comparisons to inspect.</param>
    /// <param name="logger">Logger for per-benchmark and aggregate diagnostics.</param>
    /// <param name="config">Ratio metric configuration.</param>
    /// <param name="details">The regression detection details.</param>
    /// <returns><see langword="true"/> when the stable worse-set geomean is greater than 1.05.</returns>
    internal static bool HasAggregateRatioRegression(
        BdnComparisonResult[] comparison,
        ILogger logger,
        RegressionRatioMetricConfig config,
        out RegressionDetectionResult details)
    {
        Debug.Assert(comparison != null, "Comparison input is required.");
        Debug.Assert(logger != null, "A logger is required.");
        Debug.Assert(config != null, "A ratio metric config is required.");

        Threshold relativeThreshold = Threshold.Parse(AggregateRatioRegressionThresholdText);
        RegressionResult[] notSame = BenchmarkDotNetDiffer.FindRegressions(comparison, relativeThreshold);

        List<RegressionResult> better = GetStableResults(notSame, ComparisonResult.Greater, config.DeltaSelector);
        LogRatioResults(better, logger, config, "less");

        List<RegressionResult> worse = GetStableResults(notSame, ComparisonResult.Lesser, config.DeltaSelector);
        LogRatioResults(worse, logger, config, "more");

        bool aggregateRegression = IsAggregateRatioRegression(worse, config.RatioSelector, out _);
        details = new RegressionDetectionResult(config.MetricName, relativeThreshold);
        return aggregateRegression;
    }

    private static RegressionResult[] FindRegressions(
        BdnComparisonResult[] comparison,
        double thresholdValueNs,
        Func<Benchmark, double?> metricSelector)
    {
        List<RegressionResult> results = [];
        foreach (BdnComparisonResult result in comparison
            .Where(result => metricSelector(result.BaseResult) != null && metricSelector(result.DiffResult) != null))
        {
            double baselineMetric = metricSelector(result.BaseResult)!.Value;
            double metric = metricSelector(result.DiffResult)!.Value;

            ComparisonResult conclusion;
            if (metric > thresholdValueNs && baselineMetric <= thresholdValueNs)
            {
                conclusion = ComparisonResult.Lesser;
            }
            else if (metric <= thresholdValueNs && baselineMetric > thresholdValueNs)
            {
                conclusion = ComparisonResult.Greater;
            }
            else
            {
                conclusion = ComparisonResult.Indistinguishable;
            }

            if (conclusion != ComparisonResult.Indistinguishable)
            {
                results.Add(new RegressionResult(result.Id, result.BaseResult, result.DiffResult, conclusion));
            }
        }

        return results.ToArray();
    }

    internal static bool TryGetGeometricMean(IEnumerable<RegressionResult> results, Func<RegressionResult, double> ratioSelector, out double geometricMean)
    {
        geometricMean = GetGeometricMeanOrNaN(results, ratioSelector);
        return !double.IsNaN(geometricMean);
    }

    private static double GetGeometricMeanOrNaN(IEnumerable<RegressionResult> results, Func<RegressionResult, double> ratioSelector)
    {
        int count = 0;
        double logSum = 0;

        foreach (RegressionResult result in results)
        {
            double ratio = ratioSelector(result);
            Debug.Assert(ratio >= 0 || double.IsNaN(ratio), "Benchmark ratios are non-negative unless undefined.");
            if (ratio <= 0 || double.IsNaN(ratio) || double.IsInfinity(ratio))
            {
                // Undefined and infinite ratios cannot contribute to a finite aggregate gate.
                continue;
            }

            logSum += Math.Log10(ratio);
            count++;
        }

        if (count == 0)
        {
            return double.NaN;
        }

        Debug.Assert(count > 0, "At least one numeric ratio is required before dividing.");
        return Math.Pow(10, logSum / count);
    }

    internal static bool IsAggregateRatioRegression(IEnumerable<RegressionResult> results, Func<RegressionResult, double> ratioSelector, out double geometricMean)
    {
        if (!TryGetGeometricMean(results, ratioSelector, out geometricMean))
        {
            return false;
        }

        return geometricMean > AggregateRatioRegressionThreshold;
    }

    internal static bool ExceedsRatioNoiseFloor(RegressionResult result, Func<RegressionResult, double> deltaSelector)
    {
        double deltaNs = deltaSelector(result);
        if (!ExceedsAbsoluteNoiseFloor(deltaNs))
        {
            return false;
        }

        return ExceedsStandardDeviationNoiseBand(result, deltaNs);
    }

    private static List<RegressionResult> GetStableResults(RegressionResult[] results, ComparisonResult conclusion, Func<RegressionResult, double> deltaSelector)
    {
        List<RegressionResult> stableResults = [];
        foreach (RegressionResult result in results.Where(result => result.Conclusion == conclusion).OrderBy(result => result.Id, StringComparer.Ordinal))
        {
            if (!ExceedsRatioNoiseFloor(result, deltaSelector))
            {
                continue;
            }

            stableResults.Add(result);
        }

        return stableResults;
    }

    private static void LogRatioResults(
        List<RegressionResult> results,
        ILogger logger,
        RegressionRatioMetricConfig config,
        string direction)
    {
        if (results.Count == 0)
        {
            return;
        }

        foreach (RegressionResult result in results)
        {
            double ratio = config.RatioSelector(result);
            double deltaMs = config.DeltaSelector(result) / TimeUnitConstants.NanoSecondsToMilliseconds;
            logger.LogInformation(
                "test: '{TestId}' took {Ratio:F3} times ({Delta:F2} ms) {Direction}",
                result.Id,
                ratio,
                deltaMs,
                direction);
        }

        if (TryGetGeometricMean(results, config.RatioSelector, out double geoMean))
        {
            string label = string.Equals(direction, "less", StringComparison.Ordinal) ? "better" : "worse";
            logger.LogInformation("========== {MetricName}: {Count} {Label}, geomean: {GeoMean:F3}% ==========", config.MetricName, results.Count, label, ToPercentChange(geoMean));
        }
    }

    private static double ToPercentChange(double ratio)
        => (ratio - 1D) * 100D;

    private static bool TryGetCombinedStandardDeviation(RegressionResult result, out double combinedStandardDeviationNs)
    {
        if (!TryGetStandardDeviation(result.BaseResult.Statistics, out double baseStandardDeviationNs)
            || !TryGetStandardDeviation(result.DiffResult.Statistics, out double diffStandardDeviationNs))
        {
            combinedStandardDeviationNs = double.NaN;
            return false;
        }

        combinedStandardDeviationNs = Math.Sqrt((baseStandardDeviationNs * baseStandardDeviationNs) + (diffStandardDeviationNs * diffStandardDeviationNs));
        return true;
    }

    private static bool ExceedsAbsoluteNoiseFloor(double deltaNs)
    {
        Debug.Assert(deltaNs >= 0 || double.IsNaN(deltaNs), "Benchmark deltas are non-negative unless undefined.");
        return !double.IsNaN(deltaNs) && deltaNs > AbsoluteNoiseFloorNs;
    }

    private static bool ExceedsStandardDeviationNoiseBand(RegressionResult result, double deltaNs)
    {
        if (!TryGetCombinedStandardDeviation(result, out double combinedStandardDeviationNs))
        {
            return true;
        }

        double noiseBandNs = NoiseBandStandardDeviationMultiplier * combinedStandardDeviationNs;
        return deltaNs > noiseBandNs;
    }

    private static bool TryGetStandardDeviation(Statistics? statistics, out double standardDeviationNs)
    {
        if (statistics == null)
        {
            standardDeviationNs = double.NaN;
            return false;
        }

        if (statistics.N > 1 && IsFiniteNonNegative(statistics.StandardDeviation))
        {
            standardDeviationNs = statistics.StandardDeviation;
            return true;
        }

        if (statistics.N > 1 && IsFiniteNonNegative(statistics.StandardError))
        {
            standardDeviationNs = statistics.StandardError * Math.Sqrt(statistics.N);
            return true;
        }

        global::PerfDiff.BDN.DataContracts.ConfidenceInterval? confidenceInterval = statistics.ConfidenceInterval;
        if (confidenceInterval?.N > 1 && IsFiniteNonNegative(confidenceInterval.StandardError))
        {
            standardDeviationNs = confidenceInterval.StandardError * Math.Sqrt(confidenceInterval.N);
            return true;
        }

        standardDeviationNs = double.NaN;
        return false;
    }

    private static bool IsFiniteNonNegative(double value)
        => value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
}
