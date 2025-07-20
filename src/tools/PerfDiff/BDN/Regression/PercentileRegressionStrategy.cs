using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

public class PercentileRegressionStrategy : IBenchmarkRegressionStrategy
{
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out object details)
    {
        const double thresholdMs = 250.0;
        List<string> violations = new();

        foreach (BdnComparisonResult result in comparison)
        {
            if (result.DiffResult.Statistics == null)
            {
                continue;
            }

            // Convert nanoseconds to milliseconds
            // See: https://benchmarkdotnet.org/articles/statistics.html ("All time values are reported in nanoseconds by default.")
            // See: https://github.com/dotnet/BenchmarkDotNet/blob/main/src/BenchmarkDotNet/Reports/Measurement.cs (Measurement.Nanoseconds)
            // See: https://github.com/dotnet/BenchmarkDotNet/blob/main/src/BenchmarkDotNet/Statistics/Statistics.cs (Statistics aggregates nanosecond values)
            double p95Ms = result.DiffResult.Statistics.Percentiles.P95 / 1_000_000.0;
            if (p95Ms > thresholdMs)
            {
                logger.LogInformation("test: '{Id}' P99 execution time {P99Ms:F2}ms exceeds threshold {D}ms", result.Id, p95Ms, thresholdMs);
                violations.Add($"Analyzer '{result.Id}' P99 execution time {p95Ms:F2}ms exceeds threshold {thresholdMs}ms.");
            }
        }

        details = violations;
        return violations.Count > 0;
    }
}
