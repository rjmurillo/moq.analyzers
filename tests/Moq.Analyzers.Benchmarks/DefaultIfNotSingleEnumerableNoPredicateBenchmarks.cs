using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Moq.Analyzers.Benchmarks;

[MemoryDiagnoser]
[InProcess]
public class DefaultIfNotSingleEnumerableNoPredicateBenchmarks
{
    private readonly IEnumerable<int> _source = new[] { 0 };

    [Benchmark(Baseline = true)]
    public int? Baseline() => _source.DefaultIfNotSingleBaselineMethod();

    [Benchmark]
    public int? Optimized() => _source.DefaultIfNotSingleOptimizedMethod();
}
