using DataTransferContracts;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using PerfDiff.BDN.Regression;

namespace PerfDiff.BDN
{
    public class BenchmarkComparisonService
    {
        private readonly ILogger _logger;
        private readonly List<IBenchmarkRegressionStrategy> _strategies;

        public BenchmarkComparisonService(ILogger logger)
        {
            _logger = logger;
            _strategies = new List<IBenchmarkRegressionStrategy>
            {
                new PercentageRegressionStrategy(),
                new AvgRegressionStrategy(),
                new P99RegressionStrategy(),
            };
        }

        public async Task<BenchmarkComparisonResult> CompareAsync(string baselineFolder, string resultsFolder)
        {
            BdnComparisonResult[]? comparison = await BenchmarkDotNetDiffer.TryGetBdnResultsAsync(baselineFolder, resultsFolder, _logger).ConfigureAwait(false);
            if (comparison is null)
            {
                return new BenchmarkComparisonResult(false, false);
            }

            bool regressionDetected = false;
            foreach (IBenchmarkRegressionStrategy strategy in _strategies)
            {
                if (strategy.HasRegression(comparison, _logger, out object details))
                {
                    regressionDetected = true;
                    switch (strategy)
                    {
                        case AvgRegressionStrategy:
                            foreach (string msg in (List<string>)details)
                                _logger.LogError(msg);
                            break;
                        case P99RegressionStrategy:
                            foreach (string msg in (List<string>)details)
                                _logger.LogError(msg);
                            break;
                        case PercentageRegressionStrategy:
                            _logger.LogError("Percentage-based regression detected (threshold: {PercentThreshold}).", details);
                            break;
                    }
                }
            }

            if (!regressionDetected)
            {
                _logger.LogInformation("All analyzers are within the average, P99, and percentage-based thresholds. No regressions detected.");
            }

            return new BenchmarkComparisonResult(true, regressionDetected);
        }
    }
}
