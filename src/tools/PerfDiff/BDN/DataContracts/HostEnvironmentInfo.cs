namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Provides information about the host environment for a benchmark run.
/// </summary>
public class HostEnvironmentInfo
{
    /// <summary>
    /// Gets or sets the BenchmarkDotNet caption.
    /// </summary>
    public string? BenchmarkDotNetCaption { get; set; }

    /// <summary>
    /// Gets or sets the BenchmarkDotNet version.
    /// </summary>
    public string? BenchmarkDotNetVersion { get; set; }

    /// <summary>
    /// Gets or sets the operating system version.
    /// </summary>
    public string? OsVersion { get; set; }

    /// <summary>
    /// Gets or sets the processor name.
    /// </summary>
    public string? ProcessorName { get; set; }

    /// <summary>
    /// Gets or sets the number of physical processors.
    /// </summary>
    public int? PhysicalProcessorCount { get; set; }

    /// <summary>
    /// Gets or sets the number of physical cores.
    /// </summary>
    public int? PhysicalCoreCount { get; set; }

    /// <summary>
    /// Gets or sets the number of logical cores.
    /// </summary>
    public int? LogicalCoreCount { get; set; }

    /// <summary>
    /// Gets or sets the runtime version.
    /// </summary>
    public string? RuntimeVersion { get; set; }

    /// <summary>
    /// Gets or sets the processor architecture.
    /// </summary>
    public string? Architecture { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a debugger is attached.
    /// </summary>
    public bool? HasAttachedDebugger { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether RyuJIT is enabled.
    /// </summary>
    public bool? HasRyuJit { get; set; }

    /// <summary>
    /// Gets or sets the build configuration.
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the JIT modules used.
    /// </summary>
    public string? JitModules { get; set; }

    /// <summary>
    /// Gets or sets the .NET CLI version.
    /// </summary>
    public string? DotNetCliVersion { get; set; }

    /// <summary>
    /// Gets or sets the chronometer frequency.
    /// </summary>
    public ChronometerFrequency? ChronometerFrequency { get; set; }

    /// <summary>
    /// Gets or sets the hardware timer kind.
    /// </summary>
    public string? HardwareTimerKind { get; set; }
}
