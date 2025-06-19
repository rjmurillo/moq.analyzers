using System.Globalization;

namespace Moq.Analyzers.Benchmarks;

#pragma warning disable ECS0200 // Consider using readonly instead of const for flexibility
#pragma warning disable ECS1300 // Static field should be initialized in a static constructor

internal static class Constants
{
    /// <summary>
    /// Number of code files to generate for benchmarking.
    /// Can be overridden by setting the MOQBENCH_FILES environment variable.
    /// </summary>
    public static readonly int NumberOfCodeFiles =
        int.TryParse(Environment.GetEnvironmentVariable("MOQBENCH_FILES"), CultureInfo.InvariantCulture, out int envValue) && envValue > 0
            ? envValue
            : 1_000;
}
