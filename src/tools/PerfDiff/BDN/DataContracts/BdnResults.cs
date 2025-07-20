using System.Linq;
using DataTransferContracts;

namespace PerfDiff.BDN.DataContracts;

public class BdnResults
{
    public bool Success { get; }

    public BdnResult[] Results { get; }

    public BdnResults(bool success, BdnResult?[] results)
    {
        Success = success;
        Results = results?.Where(r => r != null).ToArray() ?? Array.Empty<BdnResult>();
    }

    public void Deconstruct(out bool success, out BdnResult[] results)
    {
        success = Success;
        results = Results;
    }
}
