namespace PerfDiff.BDN.DataContracts;

public record BdnComparisonResult(string Id, Benchmark BaseResult, Benchmark DiffResult);
