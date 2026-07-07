namespace PerfDiff.Tests;

public sealed class NoiseFloorCase
{
    public double BaseMeanNs { get; init; }

    public double DiffMeanNs { get; init; }

    public double BaseStandardDeviationNs { get; init; }

    public double DiffStandardDeviationNs { get; init; }

    public int N { get; init; }

    public double DeltaNs { get; init; }

    public bool Expected { get; init; }
}
