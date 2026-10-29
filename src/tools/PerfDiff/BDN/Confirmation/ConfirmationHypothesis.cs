namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// A screen-stage regression that confirmation rounds must reproduce before the gate fails.
/// </summary>
/// <param name="BenchmarkId">The BenchmarkDotNet full name, including parameters.</param>
/// <param name="Metric">The statistic that tripped the screen gate.</param>
/// <param name="MinimumEffect">
/// The smallest slowdown that counts, as a fraction. 0.05 means the current build must be
/// more than 5% slower. Zero means any slowdown counts.
/// </param>
/// <param name="BudgetNs">
/// The fixed budget that the screen gate checked, in nanoseconds, or <see langword="null"/>
/// for a ratio gate. A budget hypothesis also needs the current build to stay over budget.
/// </param>
/// <param name="Floor">
/// The absolute slowdown floor of a ratio gate, or <see langword="null"/> for none.
/// </param>
public sealed record ConfirmationHypothesis(
    string BenchmarkId,
    ConfirmationMetric Metric,
    double MinimumEffect,
    double? BudgetNs,
    DeltaFloor? Floor = null)
{
    /// <summary>
    /// Selects the hypothesis metric from a benchmark.
    /// </summary>
    /// <param name="benchmark">The benchmark to read.</param>
    /// <returns>The metric in nanoseconds, or <see langword="null"/> when the report has no value.</returns>
    public double? SelectMetric(DataContracts.Benchmark benchmark)
        => Select(Metric, benchmark);

    /// <summary>
    /// Selects a metric from a benchmark.
    /// </summary>
    /// <param name="metric">The statistic to read.</param>
    /// <param name="benchmark">The benchmark to read.</param>
    /// <returns>The metric in nanoseconds, or <see langword="null"/> when the report has no value.</returns>
    internal static double? Select(ConfirmationMetric metric, DataContracts.Benchmark benchmark)
        => metric switch
        {
            ConfirmationMetric.Median => benchmark.Statistics?.Median,
            ConfirmationMetric.Mean => benchmark.Statistics?.Mean,
            _ => benchmark.Statistics?.Percentiles?.P95,
        };
}
