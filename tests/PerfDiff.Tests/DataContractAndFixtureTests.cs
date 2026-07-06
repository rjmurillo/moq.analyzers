using PerfDiff.BDN;
using PerfDiff.BDN.DataContracts;
using Xunit;

namespace PerfDiff.Tests;

public sealed class DataContractAndFixtureTests
{
    [Fact]
    public void BdnResults_Deconstruct_ReturnsAssignedValues()
    {
        BdnResult result = new()
        {
            Title = "title",
            Benchmarks = [BenchmarkTestData.CreateBenchmark("Benchmark.A")],
        };
        BdnResults results = new(true, [result]);

        (bool success, BdnResult?[] values) = results;

        Assert.True(success);
        Assert.Same(result, values[0]);
        Assert.Equal("Benchmark.A", result.Benchmarks!.Single().FullName);
    }

    [Fact]
    public void BenchmarkAndMeasurementContracts_RoundTripAssignedValues()
    {
        Benchmark benchmark = new()
        {
            DisplayInfo = "display",
            Namespace = "namespace",
            Type = "type",
            Method = "method",
            MethodTitle = "title",
            Parameters = "parameters",
            FullName = "Benchmark.A",
            Statistics = new Statistics { Median = 3, Mean = 4 },
            Measurements = [new Measurement { IterationStage = "Result", LaunchIndex = 1, IterationIndex = 2, Operations = 2, Nanoseconds = 20 }],
        };

        Assert.Equal("display", benchmark.DisplayInfo);
        Assert.Equal("namespace", benchmark.Namespace);
        Assert.Equal("type", benchmark.Type);
        Assert.Equal("method", benchmark.Method);
        Assert.Equal("title", benchmark.MethodTitle);
        Assert.Equal("parameters", benchmark.Parameters);
        Assert.Equal("Benchmark.A", benchmark.FullName);
        Assert.Equal(10, benchmark.GetOriginalValues()[0]);
    }

    [Fact]
    public void StatisticsContracts_RoundTripAssignedValues()
    {
        Statistics statistics = new()
        {
            N = 5,
            Min = 1,
            LowerFence = 1,
            Q1 = 2,
            Median = 3,
            Mean = 4,
            Q3 = 5,
            UpperFence = 6,
            Max = 7,
            InterquartileRange = 3,
            LowerOutliers = [0],
            UpperOutliers = [8],
            AllOutliers = [0, 8],
            StandardError = 0.1,
            Variance = 0.2,
            StandardDeviation = 0.3,
            Skewness = 0.4,
            Kurtosis = 0.5,
            ConfidenceInterval = new ConfidenceInterval { N = 5, Mean = 4, StandardError = 0.1, Level = 95, Margin = 2, Lower = 2, Upper = 6 },
        };

        Assert.Equal(5, statistics.N);
        Assert.Equal(3, statistics.Median);
        Assert.Equal(8, statistics.AllOutliers![1]);
        Assert.Equal(95, statistics.ConfidenceInterval!.Level);
    }

    [Fact]
    public void PercentilesAndMemoryContracts_RoundTripAssignedValues()
    {
        Percentiles percentiles = new() { P0 = 1, P25 = 2, P50 = 3, P67 = 4, P80 = 5, P85 = 6, P90 = 7, P95 = 8, P100 = 9 };
        Memory memory = new() { Gen0Collections = 1, Gen1Collections = 2, Gen2Collections = 3, TotalOperations = 4, BytesAllocatedPerOperation = 5 };

        Assert.Equal(1, percentiles.P0);
        Assert.Equal(5, percentiles.P80);
        Assert.Equal(8, percentiles.P95);
        Assert.Equal(9, percentiles.P100);
        Assert.Equal(1, memory.Gen0Collections);
        Assert.Equal(5, memory.BytesAllocatedPerOperation);
    }

    [Fact]
    public void HostEnvironmentContracts_RoundTripAssignedValues()
    {
        HostEnvironmentInfo info = new()
        {
            BenchmarkDotNetCaption = "caption",
            BenchmarkDotNetVersion = "version",
            OsVersion = "os",
            ProcessorName = "cpu",
            PhysicalProcessorCount = 1,
            PhysicalCoreCount = 2,
            LogicalCoreCount = 4,
            RuntimeVersion = "runtime",
            Architecture = "x64",
            HasAttachedDebugger = false,
            HasRyuJit = true,
            Configuration = "Release",
            JitModules = "jit",
            DotNetCliVersion = "10.0",
            ChronometerFrequency = new ChronometerFrequency { Hertz = 1 },
            HardwareTimerKind = "timer",
        };

        Assert.Equal("caption", info.BenchmarkDotNetCaption);
        Assert.Equal(4, info.LogicalCoreCount);
        Assert.True(info.HasRyuJit);
        Assert.Equal(1, info.ChronometerFrequency!.Hertz);
    }

    [Fact]
    public async Task SampleBenchmarkDotNetFixtures_CompareEndToEnd()
    {
        string baseline = GetTestDataPath("BdnBaseline");
        string results = GetTestDataPath("BdnResults");

        BenchmarkComparisonResult comparison = await BenchmarkDotNetDiffer.TryCompareBenchmarkDotNetResultsAsync(baseline, results, new PerfDiffTestLogger(), CancellationToken.None).ConfigureAwait(true);

        Assert.True(comparison.CompareSucceeded);
        Assert.False(comparison.RegressionDetected);
    }

    private static string GetTestDataPath(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "tests", "PerfDiff.Tests", "TestData", name);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find PerfDiff test data folder '{name}'.");
    }
}
