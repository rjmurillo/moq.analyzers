using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

[InProcess]
[MemoryDiagnoser]
public class CallbackReturnsSymbolBenchmarks
{
    private IMethodSymbol? _callbackMethod;
    private IMethodSymbol? _returnsMethod;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        (string Name, string Content)[] sources =
        [
            ("Sample", @"namespace Moq.Language { public interface ICallback { void Callback(); } public interface IReturns { void Returns(); } }")
        ];

        Compilation? compilation = await CSharpCompilationCreator.CreateAsync(sources).ConfigureAwait(false);
        _callbackMethod = compilation!.GetTypeByMetadataName("Moq.Language.ICallback")!.GetMembers("Callback").OfType<IMethodSymbol>().Single();
        _returnsMethod = compilation.GetTypeByMetadataName("Moq.Language.IReturns")!.GetMembers("Returns").OfType<IMethodSymbol>().Single();
    }

    [Benchmark(Baseline = true)]
    public bool OldCallbackCheck() => OldIsCallbackOrReturnSymbol(_callbackMethod!);

    [Benchmark]
    public bool NewCallbackCheck() => NewIsCallbackOrReturnSymbol(_callbackMethod!);

    [Benchmark]
    public bool OldReturnsCheck() => OldIsCallbackOrReturnSymbol(_returnsMethod!);

    [Benchmark]
    public bool NewReturnsCheck() => NewIsCallbackOrReturnSymbol(_returnsMethod!);

    private static bool OldIsCallbackOrReturnSymbol(IMethodSymbol symbol)
    {
        string? methodName = symbol.ToString();
        if (string.IsNullOrEmpty(methodName))
        {
            return false;
        }

        return methodName.StartsWith("Moq.Language.ICallback", StringComparison.Ordinal)
            || methodName.StartsWith("Moq.Language.IReturns", StringComparison.Ordinal);
    }

    private static bool NewIsCallbackOrReturnSymbol(IMethodSymbol symbol)
    {
        INamedTypeSymbol? containingType = symbol.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        string containingTypeName = containingType.ToDisplayString();

        bool isCallback = string.Equals(symbol.Name, "Callback", StringComparison.Ordinal)
            && containingTypeName.StartsWith("Moq.Language.ICallback", StringComparison.Ordinal);

        bool isReturns = string.Equals(symbol.Name, "Returns", StringComparison.Ordinal)
            && containingTypeName.StartsWith("Moq.Language.IReturns", StringComparison.Ordinal);

        return isCallback || isReturns;
    }
}
