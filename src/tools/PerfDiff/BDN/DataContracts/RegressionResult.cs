using Perfolizer.Mathematics.SignificanceTesting;

namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of a regression test between two benchmarks.
/// </summary>
public record RegressionResult(string Id, Benchmark BaseResult, Benchmark DiffResult, EquivalenceTestConclusion Conclusion);
