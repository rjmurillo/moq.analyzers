using DataTransferContracts;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression
{
    public class AvgRegressionStrategy : IBenchmarkRegressionStrategy
    {
        public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out object details)
        {
            const double analyzerAvgThresholdMs = 100.0;
            List<string> violations = new List<string>();

            foreach (BdnComparisonResult result in comparison)
            {
                if (result.DiffResult.Statistics == null)
                {
                    continue;
                }

                double avgMs = result.DiffResult.Statistics.Mean;
                if (avgMs > analyzerAvgThresholdMs)
                {
                    logger.LogInformation("test: '{Id}' average execution time {AvgMs:F2}ms exceeds threshold {D}ms", result.Id, avgMs, analyzerAvgThresholdMs);
                    violations.Add($"Analyzer '{result.Id}' average execution time {avgMs:F2}ms exceeds threshold {analyzerAvgThresholdMs}ms.");
                }
            }

            details = violations;
            return violations.Count > 0;
        }
    }
}
