namespace DataTransferContracts;

public class BdnResult
{
    public string? Title { get; set; }

    public HostEnvironmentInfo? HostEnvironmentInfo { get; set; }

    public IEnumerable<Benchmark>? Benchmarks { get; set; }
}
