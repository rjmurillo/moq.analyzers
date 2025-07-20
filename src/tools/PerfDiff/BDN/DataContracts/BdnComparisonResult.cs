namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of comparing two benchmarks.
/// </summary>
/// <param name="Id">Stable identifier for the benchmark (class/method/parameters).</param>
/// <param name="BaseResult">Baseline benchmark statistics.</param>
/// <param name="DiffResult">Benchmark statistics from the run under test.</param>
public record BdnComparisonResult(string Id, Benchmark BaseResult, Benchmark DiffResult);
