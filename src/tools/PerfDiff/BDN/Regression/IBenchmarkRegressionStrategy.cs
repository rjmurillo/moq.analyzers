using DataTransferContracts;
using Microsoft.Extensions.Logging;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN.Regression
{
    public interface IBenchmarkRegressionStrategy
    {
        /// <summary>
        /// Checks for regression in the provided comparison results.
        /// </summary>
        /// <param name="comparison">Array of benchmark comparison results.</param>
        /// <param name="logger">Logger for reporting.</param>
        /// <param name="details"></param>
        /// <returns>True if regression detected, false otherwise.</returns>
        bool HasRegression(BdnComparisonResult[] comparison, ILogger logger, out object details);
    }
}
