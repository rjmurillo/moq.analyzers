using BenchmarkDotNet.Attributes;

namespace Moq.Analyzers.Benchmarks;

internal abstract class BenchmarkBase
{
    [IterationSetup]
    public static void CreateCompilations()
    {
    }
}
