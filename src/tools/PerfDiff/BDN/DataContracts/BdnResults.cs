using DataTransferContracts;

namespace PerfDiff;

internal class BdnResults(bool success, BdnResult?[] results)
{
    public bool Success { get; } = success;

    public BdnResult[] Results { get; } = results;

    public void Deconstruct(out bool success, out BdnResult[] results)
    {
        success = Success;
        results = Results;
    }
}
