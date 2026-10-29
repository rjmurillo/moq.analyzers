namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// The smallest absolute slowdown that a ratio gate accepts.
/// </summary>
/// <remarks>
/// The screen's ratio gates drop a benchmark whose slowdown is under 0.5 ms, even when its
/// ratio is high. Confirmation keeps the same rule, so it confirms only what the screen counts.
/// </remarks>
/// <param name="Metric">
/// The statistic the screen checks against the floor. The P95 gate checks the mean, so a
/// tail-only spike does not count.
/// </param>
/// <param name="MinimumNs">The floor, in nanoseconds. The median paired slowdown must exceed it.</param>
public sealed record DeltaFloor(ConfirmationMetric Metric, double MinimumNs);
