using DataTransferContracts;
using Perfolizer.Mathematics.SignificanceTesting;

namespace PerfDiff.BDN.DataContracts;

public record RegressionResult(string Id, Benchmark BaseResult, Benchmark DiffResult, EquivalenceTestConclusion Conclusion);
