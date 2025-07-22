using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Moq.Analyzers.Benchmarks;

/// <summary>
/// Entrypoint for benchmarks.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entrypoint for benchmarks.
    /// </summary>
    /// <summary>
    /// Entry point for running all benchmarks in the current assembly with a custom configuration.
    /// </summary>
    /// <param name="args">Command line arguments passed to the benchmark runner.</param>
    public static void Main(string[] args)
    {
        // Needed because Microsoft.CodeAnalysis.Testing does not build with optimizations. See https://github.com/dotnet/roslyn-sdk/issues/1165.
        ManualConfig config = ManualConfig.Create(DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator))
            .AddColumn(StatisticColumn.Mean)
            .AddColumn(StatisticColumn.P95);

        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }
}
