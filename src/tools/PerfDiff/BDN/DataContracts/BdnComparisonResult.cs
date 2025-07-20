namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of comparing two benchmarks.
/// </summary>
public record BdnComparisonResult(string Id, Benchmark BaseResult, Benchmark DiffResult);
