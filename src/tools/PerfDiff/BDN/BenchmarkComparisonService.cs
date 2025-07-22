using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;

namespace PerfDiff.BDN;

/// <summary>
/// Provides comparison services for benchmark results using multiple regression strategies.
/// </summary>
public class BenchmarkComparisonService(ILogger logger)
{
    private readonly List<IBenchmarkRegressionStrategy> _strategies =
    [
        new PercentageRegressionStrategy(),
        new P95RatioRegressionStrategy(),
        new PercentileRegressionStrategy(),
        new MeanWallClockRegressionStrategy(),
        new MeanPercentageRegressionStrategy(),
    ];

    /// <summary>
    /// Compares two sets of benchmark results and detects regressions.
    /// </summary>
    /// <param name="baselineFolder">The folder containing baseline results.</param>
    /// <param name="resultsFolder">The folder containing new results.</param>
    /// <summary>
    /// Asynchronously compares benchmark results from two folders using multiple regression detection strategies.
    /// </summary>
    /// <param name="baselineFolder">The path to the folder containing baseline benchmark results.</param>
    /// <param name="resultsFolder">The path to the folder containing new benchmark results to compare against the baseline.</param>
    /// <returns>A <see cref="BenchmarkComparisonResult"/> indicating whether the comparison succeeded and if any regression was detected.</returns>
    public async Task<BenchmarkComparisonResult> CompareAsync(string baselineFolder, string resultsFolder)
    {
        BdnComparisonResult[]? comparison = await BenchmarkDotNetDiffer.TryGetBdnResultsAsync(baselineFolder, resultsFolder, logger).ConfigureAwait(false);
        if (comparison is null)
        {
            return new BenchmarkComparisonResult(CompareSucceeded: false, RegressionDetected: false);
        }

        bool regressionDetected = false;
        foreach (IBenchmarkRegressionStrategy strategy in _strategies)
        {
            if (!strategy.HasRegression(comparison, logger, out RegressionDetectionResult details))
            {
                continue;
            }

            regressionDetected = true;
            logger.LogError("========== {MetricName} regression detected (threshold: {PercentThreshold}) -- {StrategyName} ==========", details.MetricName, details.Threshold, strategy.GetType().Name);
        }

        if (!regressionDetected)
        {
            logger.LogInformation("========== No regressions detected. ==========");
        }

        return new BenchmarkComparisonResult(CompareSucceeded: true, regressionDetected);
    }
}
