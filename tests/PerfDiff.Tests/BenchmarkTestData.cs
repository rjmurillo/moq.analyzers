using System.Text.Json;
using PerfDiff.BDN.DataContracts;

namespace PerfDiff.Tests;

internal static class BenchmarkTestData
{
    public static Benchmark CreateBenchmark(
        string fullName,
        double medianNs = 100,
        double meanNs = 100,
        double p95Ns = 100,
        params Measurement[] measurements)
    {
        return new Benchmark
        {
            FullName = fullName,
            Statistics = new Statistics
            {
                Median = medianNs,
                Mean = meanNs,
                Percentiles = new Percentiles
                {
                    P95 = p95Ns,
                },
            },
            Measurements = measurements.Length == 0
                ? CreateMeasurements(100, 100, 100)
                : measurements,
        };
    }

    public static Measurement[] CreateMeasurements(params double[] nanoseconds)
        => nanoseconds
            .Select((nanosecondsValue, index) => new Measurement
            {
                IterationStage = "Result",
                IterationIndex = index,
                Operations = 1,
                Nanoseconds = nanosecondsValue,
            })
            .ToArray();

    public static Measurement CreateMeasurement(double nanoseconds, long operations)
        => new()
        {
            IterationStage = "Result",
            Operations = operations,
            Nanoseconds = nanoseconds,
        };

    public static ResultDirectory CreateResultDirectory(params Benchmark[] benchmarks)
    {
        ResultDirectory directory = new();
        BdnResult result = new()
        {
            Benchmarks = benchmarks,
        };

        File.WriteAllText(directory.ResultFilePath, JsonSerializer.Serialize(result));
        return directory;
    }

    public sealed class ResultDirectory : IDisposable
    {
        public ResultDirectory()
        {
            Path = System.IO.Path.Combine(AppContext.BaseDirectory, "PerfDiff.TestData", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
            ResultFilePath = System.IO.Path.Combine(Path, "results.full-compressed.json");
        }

        public string Path { get; }

        public string ResultFilePath { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
