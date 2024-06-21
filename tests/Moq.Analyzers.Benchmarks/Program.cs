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
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
        => BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator)); // Needed because Microsoft.CodeAnalysis.Testing does not build with optimizations. See https://github.com/dotnet/roslyn-sdk/issues/1165.
}
