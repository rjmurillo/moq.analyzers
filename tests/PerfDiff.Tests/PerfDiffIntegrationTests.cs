using System.Reflection;
using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using PerfDiff.ETL;
using Xunit;

namespace PerfDiff.Tests;

public sealed class PerfDiffIntegrationTests
{
    [Fact]
    public async Task BenchmarkComparisonService_WhenAnyStrategyFindsRegression_ReturnsRegression()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                meanNs: 10_000_000,
                p95Ns: 10_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(10_000_000, 10_000_000, 10_000_000, 10_000_000, 10_000_000)));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                meanNs: 300_000_000,
                p95Ns: 300_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(300_000_000, 300_000_000, 300_000_000, 300_000_000, 300_000_000)));
        PerfDiffTestLogger logger = new();
        BenchmarkComparisonService service = new(logger);

        BenchmarkComparisonResult comparison = await service.CompareAsync(baseline.Path, results.Path, CancellationToken.None);

        Assert.True(comparison.CompareSucceeded);
        Assert.True(comparison.RegressionDetected);
        Assert.Contains(logger.Messages, message => message.Contains("regression detected", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PerfDiffCompareAsync_WhenBenchmarkComparisonFails_ReturnsOne()
    {
        using BenchmarkTestData.ResultDirectory baseline = new();
        using BenchmarkTestData.ResultDirectory results = new();

        int exitCode = await PerfDiff.CompareAsync(baseline.Path, results.Path, failOnRegression: false, new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task PerfDiffCompareAsync_WhenNoRegression_ReturnsZero()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));

        int exitCode = await PerfDiff.CompareAsync(baseline.Path, results.Path, failOnRegression: true, new PerfDiffTestLogger(), CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PerfDiffCompareAsync_WhenEtlConfirmsRegressionAndFailOnRegressionIsFalse_ReturnsZero()
        => await AssertEtlConfirmsRegressionAsync(failOnRegression: false, expectedExitCode: 0).ConfigureAwait(true);

    [Fact]
    public async Task PerfDiffCompareAsync_WhenEtlConfirmsRegressionAndFailOnRegressionIsTrue_ReturnsOne()
        => await AssertEtlConfirmsRegressionAsync(failOnRegression: true, expectedExitCode: 1).ConfigureAwait(true);

    [Fact]
    public async Task PerfDiffCompareAsync_WhenEtlRejectsRegression_ReturnsZero()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        await File.WriteAllTextAsync(Path.Combine(baseline.Path, "trace.etl.zip"), "baseline").ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(results.Path, "trace.etl.zip"), "results").ConfigureAwait(true);

        int exitCode = await PerfDiff.CompareAsync(
            baseline.Path,
            results.Path,
            failOnRegression: true,
            new PerfDiffTestLogger(),
            CancellationToken.None,
            static (_, _, _, _) => Task.FromResult(new BenchmarkComparisonResult(CompareSucceeded: true, RegressionDetected: true)),
            static (_, _) => (CompareSucceeded: true, RegressionDetected: false)).ConfigureAwait(true);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PerfDiffCompareAsync_WhenResultsEtlIsMissing_ReturnsNonFailingRegression()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        await File.WriteAllTextAsync(Path.Combine(baseline.Path, "trace.etl.zip"), "baseline").ConfigureAwait(true);

        int exitCode = await PerfDiff.CompareAsync(
            baseline.Path,
            results.Path,
            failOnRegression: false,
            new PerfDiffTestLogger(),
            CancellationToken.None,
            static (_, _, _, _) => Task.FromResult(new BenchmarkComparisonResult(CompareSucceeded: true, RegressionDetected: true)),
            static (_, _) => (CompareSucceeded: true, RegressionDetected: true)).ConfigureAwait(true);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PerfDiffCompareAsync_WhenBaselineEtlIsMissing_ReturnsNonFailingRegression()
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        await File.WriteAllTextAsync(Path.Combine(results.Path, "trace.etl.zip"), "results").ConfigureAwait(true);

        int exitCode = await PerfDiff.CompareAsync(
            baseline.Path,
            results.Path,
            failOnRegression: false,
            new PerfDiffTestLogger(),
            CancellationToken.None,
            static (_, _, _, _) => Task.FromResult(new BenchmarkComparisonResult(CompareSucceeded: true, RegressionDetected: true)),
            static (_, _) => (CompareSucceeded: true, RegressionDetected: true)).ConfigureAwait(true);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PerfDiffCompareAsync_WhenBdnRegressionHasNoUsableEtlAndFailOnRegressionIsFalse_ReturnsZero()
        => await AssertBdnRegressionHasNoUsableEtlAsync(failOnRegression: false, expectedExitCode: 0).ConfigureAwait(true);

    [Fact]
    public async Task PerfDiffCompareAsync_WhenBdnRegressionHasNoUsableEtlAndFailOnRegressionIsTrue_ReturnsOne()
        => await AssertBdnRegressionHasNoUsableEtlAsync(failOnRegression: true, expectedExitCode: 1).ConfigureAwait(true);

    [Fact]
    public void TryGetETLPaths_AcceptsSingleEtlZipFilePath()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "PerfDiff.TestData", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string etlPath = Path.Combine(directory, "trace.etl.zip");
        File.WriteAllText(etlPath, "not an etl");
        try
        {
            MethodInfo method = typeof(PerfDiff).GetMethod("TryGetETLPaths", BindingFlags.NonPublic | BindingFlags.Static)!;
            object?[] arguments = [etlPath, new PerfDiffTestLogger(), null];

            object? found = method.Invoke(null, arguments);

            Assert.True(found is true);
            Assert.Equal(etlPath, arguments[2]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TryGetETLPaths_RejectsNonEtlZipFilePath()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "PerfDiff.TestData", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, "trace.txt");
        File.WriteAllText(filePath, "not an etl");
        try
        {
            MethodInfo method = typeof(PerfDiff).GetMethod("TryGetETLPaths", BindingFlags.NonPublic | BindingFlags.Static)!;
            object?[] arguments = [filePath, new PerfDiffTestLogger(), null];

            object? found = method.Invoke(null, arguments);

            Assert.True(found is false);
            Assert.Null(arguments[2]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TryGetETLPaths_RejectsMissingPath()
    {
        string missingPath = Path.Combine(AppContext.BaseDirectory, "PerfDiff.TestData", Guid.NewGuid().ToString("N"), "trace.etl.zip");
        MethodInfo method = typeof(PerfDiff).GetMethod("TryGetETLPaths", BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments = [missingPath, new PerfDiffTestLogger(), null];

        object? found = method.Invoke(null, arguments);

        Assert.True(found is false);
        Assert.Null(arguments[2]);
    }

    [Fact]
    public void ComputeOverweights_CoversInterestThresholdBoundaries()
    {
        Dictionary<string, float> baseline = new(StringComparer.Ordinal)
        {
            ["top"] = 100,
            ["middle"] = 25,
            ["leaf"] = 5,
            ["missing"] = 50,
        };
        Dictionary<string, float> source = new(StringComparer.Ordinal)
        {
            ["top"] = 200,
            ["middle"] = 80,
            ["leaf"] = 40,
        };

        OverWeightResult[] results = EtlDiffer.ComputeOverweights(320, source, 180, baseline).ToArray();

        Assert.Equal(3, results.Length);
        Assert.Contains(results, result => string.Equals(result.Name, "top", StringComparison.Ordinal) && result.Interest >= 1);
        Assert.Contains(results, result => string.Equals(result.Name, "middle", StringComparison.Ordinal) && result.Interest >= 2);
        Assert.Contains(results, result => string.Equals(result.Name, "leaf", StringComparison.Ordinal) && result.Interest >= 3);
        Assert.DoesNotContain(results, result => string.Equals(result.Name, "missing", StringComparison.Ordinal));
    }

    [Fact]
    public void ComputeOverweights_CoversExtremePercentAndStackPositionThresholds()
    {
        Dictionary<string, float> baseline = new(StringComparer.Ordinal)
        {
            ["root"] = 100,
            ["deep"] = 1,
        };
        Dictionary<string, float> source = new(StringComparer.Ordinal)
        {
            ["root"] = 100.5F,
            ["deep"] = 102,
        };

        OverWeightResult[] results = EtlDiffer.ComputeOverweights(101.4F, source, 100.5F, baseline).ToArray();

        Assert.Contains(results, result => string.Equals(result.Name, "root", StringComparison.Ordinal) && result.Interest <= 1);
        Assert.Contains(results, result => string.Equals(result.Name, "deep", StringComparison.Ordinal) && result.Interest >= 3);
        Assert.Contains("Overweight", results[0].ToString(), StringComparison.Ordinal);
    }

    private static async Task AssertEtlConfirmsRegressionAsync(bool failOnRegression, int expectedExitCode)
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(BenchmarkTestData.CreateBenchmark("Benchmark.A"));
        await File.WriteAllTextAsync(Path.Combine(baseline.Path, "trace.etl.zip"), "baseline").ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(results.Path, "trace.etl.zip"), "results").ConfigureAwait(true);

        int exitCode = await PerfDiff.CompareAsync(
            baseline.Path,
            results.Path,
            failOnRegression,
            new PerfDiffTestLogger(),
            CancellationToken.None,
            static (_, _, _, _) => Task.FromResult(new BenchmarkComparisonResult(CompareSucceeded: true, RegressionDetected: true)),
            static (_, _) => (CompareSucceeded: true, RegressionDetected: true)).ConfigureAwait(true);

        Assert.Equal(expectedExitCode, exitCode);
    }

    private static async Task AssertBdnRegressionHasNoUsableEtlAsync(bool failOnRegression, int expectedExitCode)
    {
        using BenchmarkTestData.ResultDirectory baseline = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                meanNs: 10_000_000,
                p95Ns: 10_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(10_000_000, 10_000_000, 10_000_000, 10_000_000, 10_000_000)));
        using BenchmarkTestData.ResultDirectory results = BenchmarkTestData.CreateResultDirectory(
            BenchmarkTestData.CreateBenchmark(
                "Benchmark.A",
                meanNs: 300_000_000,
                p95Ns: 300_000_000,
                measurements: BenchmarkTestData.CreateMeasurements(300_000_000, 300_000_000, 300_000_000, 300_000_000, 300_000_000)));
        await File.WriteAllTextAsync(Path.Combine(baseline.Path, "a.etl.zip"), "not an etl").ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(baseline.Path, "b.etl.zip"), "not an etl").ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(results.Path, "a.etl.zip"), "not an etl").ConfigureAwait(false);
        PerfDiffTestLogger logger = new();

        int exitCode = await PerfDiff.CompareAsync(baseline.Path, results.Path, failOnRegression, logger, CancellationToken.None).ConfigureAwait(false);

        Assert.Equal(expectedExitCode, exitCode);
        Assert.Contains(logger.Messages, message => message.Contains("Found 2 ETL files", StringComparison.Ordinal));
    }
}
