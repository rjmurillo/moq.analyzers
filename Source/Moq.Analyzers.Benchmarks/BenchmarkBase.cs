using BenchmarkDotNet.Attributes;

namespace Moq.Analyzers.Benchmarks;

internal abstract class BenchmarkBase
{
    protected static IDictionary<string, string> Properties { get; } = new Dictionary<string, string>
    {
        // ["build_property.TargetFramework"] = "net8.0",
        // ["build_property._SupportedPlatformList"] = "Linux,macOS",
    };

    [IterationSetup]
    public static void CreateCompilations()
    {

    }
}
