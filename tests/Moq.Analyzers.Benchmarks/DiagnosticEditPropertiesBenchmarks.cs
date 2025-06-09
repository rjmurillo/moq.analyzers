using System.Collections.Immutable;
using System.Globalization;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
public class DiagnosticEditPropertiesBenchmarks
{
    [Benchmark(Baseline = true)]
    public ImmutableDictionary<string, string?> UsingBuilder()
    {
        ImmutableDictionary<string, string?>.Builder builder = ImmutableDictionary.CreateBuilder<string, string?>(StringComparer.Ordinal);
        builder.Add("Type", "Insert");
        builder.Add("Position", 1.ToString(CultureInfo.InvariantCulture));
        return builder.ToImmutable();
    }

    [Benchmark]
    public ImmutableDictionary<string, string?> UsingDictionary()
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            { "Type", "Insert" },
            { "Position", 1.ToString(CultureInfo.InvariantCulture) },
        }.ToImmutableDictionary();
    }
}
