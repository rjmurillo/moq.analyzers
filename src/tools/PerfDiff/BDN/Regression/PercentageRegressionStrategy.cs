using DataTransferContracts;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;
using Perfolizer.Mathematics.Thresholds;

namespace PerfDiff.BDN.Regression
{
    public class PercentageRegressionStrategy : IBenchmarkRegressionStrategy
    {
        public bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out object details)
        {
            _ = Threshold.TryParse("35%", out Threshold? testThreshold);
            RegressionResult[] notSame = BenchmarkDotNetDiffer.FindRegressions(comparison, testThreshold);

            List<RegressionResult> better = notSame.Where(result => result.Conclusion == Perfolizer.Mathematics.SignificanceTesting.EquivalenceTestConclusion.Faster).ToList();
            List<RegressionResult> worse = notSame.Where(result => result.Conclusion == Perfolizer.Mathematics.SignificanceTesting.EquivalenceTestConclusion.Slower).ToList();
            int betterCount = better.Count;
            int worseCount = worse.Count;

            // Exclude Infinity ratios
            better = better.Where(x => !double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetRatio(x))).ToList();
            worse = worse.Where(x => !double.IsPositiveInfinity(BenchmarkDotNetDiffer.GetRatio(x))).ToList();

            if (betterCount > 0)
            {
                double betterGeoMean = Math.Pow(10, better.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetRatio(better[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetRatio(y))) / betterCount);
                logger.LogInformation("better: {BetterCount}, geomean: {BetterGeoMean:F3}%", betterCount, betterGeoMean);
                foreach (RegressionResult betterResult in better)
                {
                    double mean = BenchmarkDotNetDiffer.GetRatio(betterResult.Conclusion, betterResult.BaseResult, betterResult.DiffResult);
                    logger.LogInformation("test: '{BetterId}' tool '{Mean:F3}' times less", betterResult.Id, mean);
                }
            }

            if (worseCount > 0)
            {
                double worseGeoMean = Math.Pow(10, worse.Skip(1).Aggregate(Math.Log10(BenchmarkDotNetDiffer.GetRatio(worse[0])), (x, y) => x + Math.Log10(BenchmarkDotNetDiffer.GetRatio(y))) / worseCount);
                logger.LogInformation("worse: {WorseCount}, geomean: {WorseGeoMean:F3}%", worseCount, worseGeoMean);
                foreach (RegressionResult worseResult in worse)
                {
                    double mean = BenchmarkDotNetDiffer.GetRatio(worseResult.Conclusion, worseResult.BaseResult, worseResult.DiffResult);
                    logger.LogInformation("test: '{WorseId}' took '{Mean:F3}' times longer", worseResult.Id, mean);
                }
            }

            details = testThreshold;
            return worseCount > 0;
        }
    }
}
