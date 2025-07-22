using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Defines a strategy for detecting regressions in benchmark results.
/// </summary>
public interface IBenchmarkRegressionStrategy
{
    /// <summary>
    /// Checks for regression in the provided comparison results.
    /// </summary>
    /// <param name="comparison">Array of benchmark comparison results.</param>
    /// <param name="logger">Logger for reporting.</param>
    /// <param name="details">Details about the regression, if detected.</param>
    /// <summary>
/// Determines whether any regressions are present in the provided benchmark comparison results.
/// </summary>
/// <param name="comparison">An array of benchmark comparison results to analyze for regressions.</param>
/// <param name="logger">Logger used for recording diagnostic or informational messages during regression detection.</param>
/// <param name="details">Outputs detailed information about the detected regression, if any.</param>
/// <returns><see langword="true"/> if regression detected; otherwise, <see langword="false"/>.</returns>
    bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out RegressionDetectionResult details);
}
