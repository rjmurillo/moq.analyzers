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
    public void Setup()
    {
        (string Name, string Content)[] sources =
        [
            ("Sample", @"namespace Moq.Language { public interface ICallback { void Callback(); } public interface IReturns { void Returns(); } }")
        ];

        Compilation? compilation = CSharpCompilationCreator.CreateAsync(sources).GetAwaiter().GetResult();
        if (compilation is null)
        {
            throw new InvalidOperationException("Failed to create C# compilation for benchmark sources.");
        }

        INamedTypeSymbol? callbackType = compilation.GetTypeByMetadataName("Moq.Language.ICallback");
        if (callbackType is null)
        {
            throw new InvalidOperationException("Type 'Moq.Language.ICallback' not found in compilation.");
        }

        IMethodSymbol[] callbackMembers = callbackType.GetMembers("Callback").OfType<IMethodSymbol>().ToArray();
        if (callbackMembers.Length != 1)
        {
            throw new InvalidOperationException($"Expected exactly one 'Callback' method in 'Moq.Language.ICallback', found {callbackMembers.Length}.");
        }

        _callbackMethod = callbackMembers[0];

        INamedTypeSymbol? returnsType = compilation.GetTypeByMetadataName("Moq.Language.IReturns");
        if (returnsType is null)
        {
            throw new InvalidOperationException("Type 'Moq.Language.IReturns' not found in compilation.");
        }

        IMethodSymbol[] returnsMembers = returnsType.GetMembers("Returns").OfType<IMethodSymbol>().ToArray();
        if (returnsMembers.Length != 1)
        {
            throw new InvalidOperationException($"Expected exactly one 'Returns' method in 'Moq.Language.IReturns', found {returnsMembers.Length}.");
        }

        _returnsMethod = returnsMembers[0];
    }

    [Benchmark(Baseline = true)]
    public bool OldCallbackCheck()
    {
        if (_callbackMethod is null)
        {
            throw new InvalidOperationException("_callbackMethod is null. Ensure Setup completed successfully.");
        }

        return OldIsCallbackOrReturnSymbol(_callbackMethod);
    }

    [Benchmark]
    public bool NewCallbackCheck()
    {
        if (_callbackMethod is null)
        {
            throw new InvalidOperationException("_callbackMethod is null. Ensure Setup completed successfully.");
        }

        return NewIsCallbackOrReturnSymbol(_callbackMethod);
    }

    [Benchmark]
    public bool OldReturnsCheck()
    {
        if (_returnsMethod is null)
        {
            throw new InvalidOperationException("_returnsMethod is null. Ensure Setup completed successfully.");
        }

        return OldIsCallbackOrReturnSymbol(_returnsMethod);
    }

    [Benchmark]
    public bool NewReturnsCheck()
    {
        if (_returnsMethod is null)
        {
            throw new InvalidOperationException("_returnsMethod is null. Ensure Setup completed successfully.");
        }

        return NewIsCallbackOrReturnSymbol(_returnsMethod);
    }

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
