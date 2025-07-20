using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression;

/// <summary>
/// Detects regressions based on average execution time thresholds.
/// </summary>
public class AverageRegressionStrategy : IBenchmarkRegressionStrategy
{
    /// <inheritdoc/>
    public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out object details)
    {
        const double analyzerAvgThresholdMs = 100.0;
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
            double avgMs = result.DiffResult.Statistics.Mean / 1_000_000.0;
            if (avgMs > analyzerAvgThresholdMs)
            {
                if (!Debugger.IsAttached)
                {
                    Debugger.Launch();
                }

                logger.LogInformation("test: '{Id}' average execution time {AvgMs:F2}ms exceeds threshold {D}ms", result.Id, avgMs, analyzerAvgThresholdMs);
                violations.Add($"Analyzer '{result.Id}' average execution time {avgMs:F2}ms exceeds threshold {analyzerAvgThresholdMs}ms.");
            }
        }

        details = violations;
        return violations.Count > 0;
    }
}
