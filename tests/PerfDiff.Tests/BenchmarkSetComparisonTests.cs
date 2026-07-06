using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using Xunit;

namespace PerfDiff.Tests;

public sealed class BenchmarkSetComparisonTests
{
    [Fact]
    public async Task CompareAsync_WithMismatchedBenchmarkSets_FailsComparison()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.B"));
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None);

        Assert.False(comparison.CompareSucceeded);
        Assert.False(comparison.RegressionDetected);
        Assert.Contains(logger.Messages, message => message.Contains("Benchmark result sets do not match", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompareAsync_WithMatchingBenchmarkSets_Succeeds()
    {
        Benchmark benchmark = BenchmarkTestData.CreateBenchmark("Benchmark.A");
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(benchmark);
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(benchmark);
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None);

        Assert.True(comparison.CompareSucceeded);
    }

    [Fact]
    public async Task CompareAsync_WithEmptyBenchmarkSet_FailsComparison()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory();
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None);

        Assert.False(comparison.CompareSucceeded);
        Assert.False(comparison.RegressionDetected);
    }

    [Fact]
    public async Task CompareAsync_WithMissingBaselinePath_FailsComparison()
    {
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(Path.Combine(results.Path, "missing"), results.Path, CancellationToken.None);

        Assert.False(comparison.CompareSucceeded);
        Assert.Contains(logger.Messages, message => message.Contains("Provided path does NOT exist", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompareAsync_WithMissingResultsPath_FailsComparison()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, Path.Combine(baseline.Path, "missing"), CancellationToken.None);

        Assert.False(comparison.CompareSucceeded);
        Assert.Contains(logger.Messages, message => message.Contains("Provided path does NOT exist", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CompareAsync_WithNoResultFiles_FailsComparison()
    {
        using BenchmarkTestData.ResultDirectory baseline = new();
        using BenchmarkTestData.ResultDirectory results = new();
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None);

        Assert.False(comparison.CompareSucceeded);
        Assert.Contains(logger.Messages, message => message.Contains("contained no 'full-compressed.json' files", StringComparison.Ordinal));
    }
}
