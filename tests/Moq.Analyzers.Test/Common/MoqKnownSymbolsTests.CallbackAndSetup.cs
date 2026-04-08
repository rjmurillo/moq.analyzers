using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public partial class MoqKnownSymbolsTests
{
    [Fact]
    public void ICallback_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ICallback);
    }

    [Fact]
    public void ICallback1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ICallback1);
    }

    [Fact]
    public void ICallback2_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ICallback2);
    }

    [Fact]
    public void ISetupGetter_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ISetupGetter);
    }

    [Fact]
    public void ISetupSetter_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ISetupSetter);
    }

    [Fact]
    public void ISetup1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ISetup1);
    }

    [Fact]
    public void ISetupPhrase1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ISetupPhrase1);
    }

    [Fact]
    public void ISetupGetter1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ISetupGetter1);
    }

    [Fact]
    public void ISetupSetter1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.ISetupSetter1);
    }

    [Fact]
    public void VoidSetupPhrase1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.VoidSetupPhrase1);
    }

    [Fact]
    public void NonVoidSetupPhrase2_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.NonVoidSetupPhrase2);
    }

    [Fact]
    public void ICallbackCallback_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ICallbackCallback.IsEmpty);
    }

    [Fact]
    public void ICallback1Callback_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ICallback1Callback.IsEmpty);
    }

    [Fact]
    public void ICallback2Callback_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ICallback2Callback.IsEmpty);
    }

    [Fact]
    public async Task ICallback_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.ICallback);
        Assert.Equal("ICallback", symbols.ICallback!.Name);
    }

    [Fact]
    public async Task ICallbackCallback_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.ICallbackCallback.IsEmpty);
    }

    [Fact]
    public async Task ISetup1_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.ISetup1);
        Assert.Equal(1, symbols.ISetup1!.Arity);
    }
}
