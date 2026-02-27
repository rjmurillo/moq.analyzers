using Perfolizer.Mathematics.Common;

namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of a regression test between two benchmarks.
/// </summary>
/// <param name="Id">The unique identifier for the regression result.</param>
/// <param name="BaseResult">The benchmark result used as the baseline for comparison.</param>
/// <param name="DiffResult">The benchmark result being compared against the baseline.</param>
/// <param name="Conclusion">The outcome of the equivalence test between the two benchmarks.</param>
public record RegressionResult(string Id, Benchmark BaseResult, Benchmark DiffResult, ComparisonResult Conclusion);
