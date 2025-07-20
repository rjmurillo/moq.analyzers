namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents statistical data for a benchmark run.
/// </summary>
public class Statistics
{
    /// <summary>
    /// Gets or sets the number of measurements.
    /// </summary>
    public int N { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Gets or sets the lower fence value for outlier detection.
    /// </summary>
    public double LowerFence { get; set; }

    /// <summary>
    /// Gets or sets the first quartile value.
    /// </summary>
    public double Q1 { get; set; }

    /// <summary>
    /// Gets or sets the median value.
    /// </summary>
    public double Median { get; set; }

    /// <summary>
    /// Gets or sets the mean value.
    /// </summary>
    public double Mean { get; set; }

    /// <summary>
    /// Gets or sets the third quartile value.
    /// </summary>
    public double Q3 { get; set; }

    /// <summary>
    /// Gets or sets the upper fence value for outlier detection.
    /// </summary>
    public double UpperFence { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Max { get; set; }

    /// <summary>
    /// Gets or sets the interquartile range.
    /// </summary>
    public double InterquartileRange { get; set; }

    /// <summary>
    /// Gets or sets the list of lower outlier values.
    /// </summary>
    public List<double>? LowerOutliers { get; set; }

    /// <summary>
    /// Gets or sets the list of upper outlier values.
    /// </summary>
    public List<double>? UpperOutliers { get; set; }

    /// <summary>
    /// Gets or sets the list of all outlier values.
    /// </summary>
    public List<double>? AllOutliers { get; set; }

    /// <summary>
    /// Gets or sets the standard error of the mean.
    /// </summary>
    public double StandardError { get; set; }

    /// <summary>
    /// Gets or sets the variance.
    /// </summary>
    public double Variance { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation.
    /// </summary>
    public double StandardDeviation { get; set; }

    /// <summary>
    /// Gets or sets the skewness value.
    /// </summary>
    public double? Skewness { get; set; }

    /// <summary>
    /// Gets or sets the kurtosis value.
    /// </summary>
    public double? Kurtosis { get; set; }

    /// <summary>
    /// Gets or sets the confidence interval for the mean.
    /// </summary>
    public ConfidenceInterval? ConfidenceInterval { get; set; }

    /// <summary>
    /// Gets or sets the percentiles for the data set.
    /// </summary>
    public Percentiles? Percentiles { get; set; }
}
