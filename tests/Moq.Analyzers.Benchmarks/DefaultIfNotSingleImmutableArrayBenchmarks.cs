using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Moq.Analyzers.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class DefaultIfNotSingleImmutableArrayBenchmarks
{
    private readonly ImmutableArray<int> _source = ImmutableArray.CreateRange(Enumerable.Range(0, 100));

#pragma warning disable ECS0900 // Minimize boxing and unboxing
    [Benchmark(Baseline = true)]
    public int? Baseline() => _source.DefaultIfNotSingleBaselineMethod(x => x == 50);

    [Benchmark]
    public int? Optimized() => _source.DefaultIfNotSingleOptimizedMethod(x => x == 50);
#pragma warning restore ECS0900
}
