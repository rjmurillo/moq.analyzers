using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public partial class MoqKnownSymbolsTests
{
    [Fact]
    public void IRaiseable_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.IRaiseable);
    }

    [Fact]
    public void IRaiseableAsync_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.IRaiseableAsync);
    }

    [Fact]
    public void IRaise1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.IRaise1);
    }

    [Fact]
    public void ICallbackRaises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ICallbackRaises.IsEmpty);
    }

    [Fact]
    public void ICallback1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ICallback1Raises.IsEmpty);
    }

    [Fact]
    public void ICallback2Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ICallback2Raises.IsEmpty);
    }

    [Fact]
    public void ISetupGetterRaises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ISetupGetterRaises.IsEmpty);
    }

    [Fact]
    public void ISetupSetterRaises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ISetupSetterRaises.IsEmpty);
    }

    [Fact]
    public void IReturnsRaises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IReturnsRaises.IsEmpty);
    }

    [Fact]
    public void IReturns1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IReturns1Raises.IsEmpty);
    }

    [Fact]
    public void IReturns2Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IReturns2Raises.IsEmpty);
    }

    [Fact]
    public void ISetup1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ISetup1Raises.IsEmpty);
    }

    [Fact]
    public void ISetupPhrase1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ISetupPhrase1Raises.IsEmpty);
    }

    [Fact]
    public void ISetupGetter1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ISetupGetter1Raises.IsEmpty);
    }

    [Fact]
    public void ISetupSetter1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ISetupSetter1Raises.IsEmpty);
    }

    [Fact]
    public void VoidSetupPhrase1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.VoidSetupPhrase1Raises.IsEmpty);
    }

    [Fact]
    public void NonVoidSetupPhrase2Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.NonVoidSetupPhrase2Raises.IsEmpty);
    }

    [Fact]
    public void IRaiseableRaises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IRaiseableRaises.IsEmpty);
    }

    [Fact]
    public void IRaiseableAsyncRaisesAsync_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IRaiseableAsyncRaisesAsync.IsEmpty);
    }

    [Fact]
    public void IRaise1Raises_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IRaise1Raises.IsEmpty);
    }

    [Fact]
    public void IRaise1RaisesAsync_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.IRaise1RaisesAsync.IsEmpty);
    }

    [Fact]
    public async Task IRaiseable_WithMoqReference_ReturnsNullForMoq4()
    {
        // IRaiseable was added after Moq 4.18.4; verify graceful null return.
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.Null(symbols.IRaiseable);
    }

    [Fact]
    public async Task IRaiseableRaises_WithMoqReference_ReturnsEmptyForMoq4()
    {
        // IRaiseable not present in Moq 4.18.4, so Raises is empty.
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.True(symbols.IRaiseableRaises.IsEmpty);
    }

    [Fact]
    public async Task IRaise1_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.IRaise1);
        Assert.Equal(1, symbols.IRaise1!.Arity);
    }
}
