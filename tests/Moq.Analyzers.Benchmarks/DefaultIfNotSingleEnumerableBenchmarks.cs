using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Moq.Analyzers.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class DefaultIfNotSingleEnumerableBenchmarks
{
    private readonly IEnumerable<int> _source = Enumerable.Range(0, 100).ToArray();

    [Benchmark(Baseline = true)]
    public int? Baseline() => _source.DefaultIfNotSingleBaselineMethod(x => x == 50);

    [Benchmark]
    public int? Optimized() => _source.DefaultIfNotSingleOptimizedMethod(x => x == 50);
}
