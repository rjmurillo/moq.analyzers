using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Columns;

namespace Moq.Analyzers.Benchmarks;

/// <summary>
/// Entrypoint for benchmarks.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entrypoint for benchmarks.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
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
