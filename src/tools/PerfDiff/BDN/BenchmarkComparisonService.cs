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
        new AverageRegressionStrategy(),
        new PercentileRegressionStrategy()
    ];

    /// <summary>
    /// Compares two sets of benchmark results and detects regressions.
    /// </summary>
    /// <param name="baselineFolder">The folder containing baseline results.</param>
    /// <param name="resultsFolder">The folder containing new results.</param>
    /// <returns>A <see cref="BenchmarkComparisonResult"/> indicating comparison success and regression detection.</returns>
    public async Task<BenchmarkComparisonResult> CompareAsync(string baselineFolder, string resultsFolder)
    {
        BdnComparisonResult[]? comparison = await BenchmarkDotNetDiffer.TryGetBdnResultsAsync(baselineFolder, resultsFolder, logger).ConfigureAwait(false);
        if (comparison is null)
        {
            return new BenchmarkComparisonResult(false, false);
        }

        bool regressionDetected = false;
        foreach (IBenchmarkRegressionStrategy strategy in _strategies)
        {
            if (strategy.HasRegression(comparison, logger, out object details))
            {
                regressionDetected = true;
                switch (strategy)
                {
                    case AverageRegressionStrategy:
                    case PercentileRegressionStrategy:
                        foreach (string msg in (List<string>)details)
                            logger.LogError(msg);
                        break;
                    case PercentageRegressionStrategy:
                        logger.LogError("Percentage-based regression detected (threshold: {PercentThreshold}).", details);
                        break;
                }
            }
        }

        if (!regressionDetected)
        {
            logger.LogInformation("All analyzers are within the average, P99, and percentage-based thresholds. No regressions detected.");
        }

        return new BenchmarkComparisonResult(true, regressionDetected);
    }
}
