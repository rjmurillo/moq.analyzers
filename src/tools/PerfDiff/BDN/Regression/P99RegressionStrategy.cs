using DataTransferContracts;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

public class P99RegressionStrategy : IBenchmarkRegressionStrategy
{
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out object details)
    {
        const double analyzerP99ThresholdMs = 250.0;
        List<string> violations = new List<string>();

        foreach (BdnComparisonResult result in comparison)
        {
            if (result.DiffResult.Statistics == null)
            {
                continue;
            }

            double p95Ms = result.DiffResult.Statistics.Percentiles.P95;
            if (p95Ms > analyzerP99ThresholdMs)
            {
                logger.LogInformation("test: '{Id}' P99 execution time {P99Ms:F2}ms exceeds threshold {D}ms", result.Id, p95Ms, analyzerP99ThresholdMs);
                violations.Add($"Analyzer '{result.Id}' P99 execution time {p95Ms:F2}ms exceeds threshold {analyzerP99ThresholdMs}ms.");
            }
        }

        details = violations;
        return violations.Count > 0;
    }
}
