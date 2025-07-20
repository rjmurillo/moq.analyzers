using DataTransferContracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.BDN
{
    public static class BenchmarkFileReader
    {
        public static async Task<BdnResult?> ReadFromFileAsync(string resultFilePath, ILogger logger)
        {
            try
            {
                return JsonConvert.DeserializeObject<BdnResult>(await File.ReadAllTextAsync(resultFilePath).ConfigureAwait(false));
            }
            catch (JsonSerializationException)
            {
                logger.LogError("Exception while reading the {ResultFilePath} file.", resultFilePath);
                return null;
            }
        }

        public static async Task<BdnResults> TryGetBdnResultAsync(string[] paths, ILogger logger)
        {
            BdnResult?[] results = await Task.WhenAll(paths.Select(path => ReadFromFileAsync(path, logger))).ConfigureAwait(false);
            return new BdnResults(!results.Any(x => x is null), results);
        }
    }
}
