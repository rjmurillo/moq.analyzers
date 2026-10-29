using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// One baseline run and one current run, keyed by benchmark full name.
/// </summary>
/// <param name="Baseline">The baseline build's benchmarks for this round.</param>
/// <param name="Current">The current build's benchmarks for this round.</param>
internal sealed record ConfirmationRound(
    IReadOnlyDictionary<string, Benchmark> Baseline,
    IReadOnlyDictionary<string, Benchmark> Current);
