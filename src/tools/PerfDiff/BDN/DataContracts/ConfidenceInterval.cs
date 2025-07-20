namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents a confidence interval for a statistical estimate.
/// </summary>
public class ConfidenceInterval
{
    /// <summary>
    /// Gets or sets the sample size.
    /// </summary>
    public int N { get; set; }

    /// <summary>
    /// Gets or sets the mean value.
    /// </summary>
    public double Mean { get; set; }

    /// <summary>
    /// Gets or sets the standard error of the mean.
    /// </summary>
    public double StandardError { get; set; }

    /// <summary>
    /// Gets or sets the confidence level (e.g., 95).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the margin of error.
    /// </summary>
    public double Margin { get; set; }

    /// <summary>
    /// Gets or sets the lower bound of the confidence interval.
    /// </summary>
    public double Lower { get; set; }

    /// <summary>
    /// Gets or sets the upper bound of the confidence interval.
    /// </summary>
    public double Upper { get; set; }
}
