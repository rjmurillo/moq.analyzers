using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public partial class MoqKnownSymbolsTests
{
    [Fact]
    public void IReturns_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.IReturns);
    }

    [Fact]
    public void IReturns1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.IReturns1);
    }

    [Fact]
    public void IReturns2_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.IReturns2);
    }

    [Fact]
    public void IThrows_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.IThrows);
    }

    [Fact]
    public void ReturnsExtensions_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ReturnsExtensions);
    }

    [Fact]
    public void IReturnsReturns_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IReturnsReturns.IsEmpty);
    }

    [Fact]
    public void IReturns1Returns_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IReturns1Returns.IsEmpty);
    }

    [Fact]
    public void IReturns2Returns_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IReturns2Returns.IsEmpty);
    }

    [Fact]
    public void IThrowsThrows_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IThrowsThrows.IsEmpty);
    }

    [Fact]
    public void ReturnsExtensionsReturnsAsync_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ReturnsExtensionsReturnsAsync.IsEmpty);
    }

    [Fact]
    public void ReturnsExtensionsThrowsAsync_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ReturnsExtensionsThrowsAsync.IsEmpty);
    }

    [Fact]
    public async Task IReturns2_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.IReturns2);
        Assert.Equal(2, symbols.IReturns2!.Arity);
    }

    [Fact]
    public async Task IReturns2Returns_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.IReturns2Returns.IsEmpty);
    }

    [Fact]
    public async Task IThrows_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.IThrows);
        Assert.Equal("IThrows", symbols.IThrows!.Name);
    }

    [Fact]
    public async Task IThrowsThrows_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.IThrowsThrows.IsEmpty);
    }

    [Fact]
    public async Task ReturnsExtensions_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.ReturnsExtensions);
        Assert.Equal("ReturnsExtensions", symbols.ReturnsExtensions!.Name);
    }

    [Fact]
    public async Task ReturnsExtensionsReturnsAsync_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.ReturnsExtensionsReturnsAsync.IsEmpty);
    }

    [Fact]
    public async Task ReturnsExtensionsThrowsAsync_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.ReturnsExtensionsThrowsAsync.IsEmpty);
    }
}
