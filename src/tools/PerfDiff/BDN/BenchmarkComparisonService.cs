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
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <param name="confirmOnlyIds">
    /// When not <see langword="null"/>, only benchmarks with these ids are gated. Used by the confirmation rerun:
    /// a strategy trips only when it trips on the benchmarks that were also flagged in the first round.
    /// </param>
    /// <returns>A <see cref="BenchmarkComparisonResult"/> indicating comparison success and regression detection.</returns>
    public async Task<BenchmarkComparisonResult> CompareAsync(
        string baselineFolder,
        string resultsFolder,
        CancellationToken cancellationToken,
        IReadOnlySet<string>? confirmOnlyIds = null)
    {
        BdnComparisonResult[]? comparison = await BenchmarkDotNetDiffer.TryGetBdnResultsAsync(baselineFolder, resultsFolder, logger, cancellationToken).ConfigureAwait(false);
        if (comparison is null)
        {
            return new BenchmarkComparisonResult(CompareSucceeded: false, RegressionDetected: false);
        }

        if (confirmOnlyIds is not null)
        {
            comparison = RestrictToConfirmedIds(comparison, confirmOnlyIds);
        }

        bool regressionDetected = false;
        SortedSet<string> regressedIds = new(StringComparer.Ordinal);
        foreach (IBenchmarkRegressionStrategy strategy in _strategies)
        {
            if (!strategy.HasRegression(comparison, logger, out RegressionDetectionResult details))
            {
                continue;
            }

            regressionDetected = true;
            regressedIds.UnionWith(details.RegressedIds);
            logger.LogError("========== {MetricName} regression detected (threshold: {PercentThreshold}) -- {StrategyName} ==========", details.MetricName, details.Threshold, strategy.GetType().Name);
        }

        if (!regressionDetected)
        {
            logger.LogInformation("========== No regressions detected. ==========");
        }

        return new BenchmarkComparisonResult(CompareSucceeded: true, regressionDetected) { RegressedIds = regressedIds.ToArray() };
    }

    private BdnComparisonResult[] RestrictToConfirmedIds(BdnComparisonResult[] comparison, IReadOnlySet<string> confirmOnlyIds)
    {
        BdnComparisonResult[] restricted = comparison.Where(result => confirmOnlyIds.Contains(result.Id)).ToArray();
        logger.LogInformation("Confirmation round: gating {Kept} of {Total} benchmarks that were also flagged in the first round.", restricted.Length, comparison.Length);
        return restricted;
    }
}
