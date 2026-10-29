namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// The benchmark statistic that a confirmation hypothesis compares.
/// </summary>
public enum ConfirmationMetric
{
    /// <summary>The median of the result iterations.</summary>
    Median,

    /// <summary>The mean of the result iterations.</summary>
    Mean,

    /// <summary>The 95th percentile of the result iterations.</summary>
    P95,
}
