#nullable disable
namespace PerfDiff
{
}

namespace DataTransferContracts
{
    public class BdnResult
    {
        public string Title { get; set; }
        public HostEnvironmentInfo HostEnvironmentInfo { get; set; }
        public List<Benchmark> Benchmarks { get; set; }
    }
}
