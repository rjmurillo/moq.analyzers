namespace DataTransferContracts;

public class HostEnvironmentInfo
{
    public string? BenchmarkDotNetCaption { get; set; }

    public string? BenchmarkDotNetVersion { get; set; }

    public string? OsVersion { get; set; }

    public string? ProcessorName { get; set; }

    public int? PhysicalProcessorCount { get; set; }

    public int? PhysicalCoreCount { get; set; }

    public int? LogicalCoreCount { get; set; }

    public string? RuntimeVersion { get; set; }

    public string? Architecture { get; set; }

    public bool? HasAttachedDebugger { get; set; }

    public bool? HasRyuJit { get; set; }

    public string? Configuration { get; set; }

    public string? JitModules { get; set; }

    public string? DotNetCliVersion { get; set; }

    public ChronometerFrequency? ChronometerFrequency { get; set; }

    public string? HardwareTimerKind { get; set; }
}
