using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public partial class MoqKnownSymbolsTests
{
    [Fact]
    public async Task MockAs_RepeatedAccess_ReturnsSameInstance()
    {
        // Without caching, each access creates a new ImmutableArray via ToImmutableArray().
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();

        ImmutableArray<IMethodSymbol> first = symbols.MockAs;
        ImmutableArray<IMethodSymbol> second = symbols.MockAs;

        // Guard: confirms symbols resolved (empty arrays share a static backing array).
        Assert.False(first.IsEmpty);

        // ImmutableArray operator== checks whether the backing arrays are the same object.
        Assert.True(first == second);
    }

    [Fact]
    public async Task MockRepositoryCreate_RepeatedAccess_ReturnsSameInstance()
    {
        // Covers the inherited-methods path (base type walking).
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();

        ImmutableArray<IMethodSymbol> first = symbols.MockRepositoryCreate;
        ImmutableArray<IMethodSymbol> second = symbols.MockRepositoryCreate;

        Assert.False(first.IsEmpty);
        Assert.True(first == second);
    }

    [Fact]
    public async Task MockBehaviorStrict_RepeatedAccess_ReturnsSameInstance()
    {
        // Covers the enum field lookup path. Note: Roslyn returns the same IFieldSymbol
        // instance from GetMembers on the same compilation, so Assert.Same validates
        // caching indirectly (the Lazy wrapper prevents re-evaluation).
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();

        IFieldSymbol? first = symbols.MockBehaviorStrict;
        IFieldSymbol? second = symbols.MockBehaviorStrict;

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void MockAs_WithoutMoqReference_RepeatedAccess_ReturnsSameEmptyInstance()
    {
        // Verifies caching for the null-type fallback (empty array).
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();

        ImmutableArray<IMethodSymbol> first = symbols.MockAs;
        ImmutableArray<IMethodSymbol> second = symbols.MockAs;

        Assert.True(first.IsEmpty);
        Assert.True(first == second);
    }

    [Fact]
    public async Task Mock1Setup_ConcurrentAccess_ReturnsSameInstance()
    {
        // Regression guard: multiple tasks accessing the same property get the same cached instance.
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        const int threadCount = 10;
        ImmutableArray<IMethodSymbol>[] results = new ImmutableArray<IMethodSymbol>[threadCount];

        Task[] tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() => results[index] = symbols.Mock1Setup);
        }

        await Task.WhenAll(tasks);

        ImmutableArray<IMethodSymbol> expected = results[0];
        Assert.False(expected.IsEmpty);
        for (int i = 1; i < threadCount; i++)
        {
            Assert.True(expected == results[i]);
        }
    }

    [Fact]
    public async Task MockBehaviorStrict_ConcurrentAccess_ReturnsSameInstance()
    {
        // Regression guard: multiple tasks accessing the same property get the same cached instance.
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        const int threadCount = 10;
        IFieldSymbol?[] results = new IFieldSymbol?[threadCount];

        Task[] tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() => results[index] = symbols.MockBehaviorStrict);
        }

        await Task.WhenAll(tasks);

        Assert.NotNull(results[0]);
        for (int i = 1; i < threadCount; i++)
        {
            Assert.Same(results[0], results[i]);
        }
    }

    [Fact]
    public void Task_WithCoreReferences_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.NotNull(symbols.Task);
        Assert.Equal("Task", symbols.Task!.Name);
    }

    [Fact]
    public void Task1_WithCoreReferences_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.NotNull(symbols.Task1);
        Assert.Equal("Task", symbols.Task1!.Name);
        Assert.Equal(1, symbols.Task1.Arity);
    }

    [Fact]
    public void ValueTask_WithCoreReferences_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.NotNull(symbols.ValueTask);
        Assert.Equal("ValueTask", symbols.ValueTask!.Name);
    }

    [Fact]
    public void ValueTask1_WithCoreReferences_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.NotNull(symbols.ValueTask1);
        Assert.Equal("ValueTask", symbols.ValueTask1!.Name);
        Assert.Equal(1, symbols.ValueTask1.Arity);
    }

    [Fact]
    public void EventHandler1_WithCoreReferences_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.NotNull(symbols.EventHandler1);
        Assert.Equal("EventHandler", symbols.EventHandler1!.Name);
        Assert.Equal(1, symbols.EventHandler1.Arity);
    }

    [Fact]
    public void Action0_WithCoreReferences_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.NotNull(symbols.Action0);
        Assert.Equal("Action", symbols.Action0!.Name);
    }

    [Fact]
    public void Action1_WithCoreReferences_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.NotNull(symbols.Action1);
        Assert.Equal("Action", symbols.Action1!.Name);
        Assert.Equal(1, symbols.Action1.Arity);
    }
}
