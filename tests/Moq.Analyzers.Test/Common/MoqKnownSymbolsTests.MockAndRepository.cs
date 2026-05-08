using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public partial class MoqKnownSymbolsTests
{
    [Fact]
    public void Mock_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.Mock);
    }

    [Fact]
    public void Mock1_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.Mock1);
    }

    [Fact]
    public void MockRepository_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.MockRepository);
    }

    [Fact]
    public void MockBehavior_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.MockBehavior);
    }

    [Fact]
    public void MockBehaviorStrict_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.MockBehaviorStrict);
    }

    [Fact]
    public void MockBehaviorLoose_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.MockBehaviorLoose);
    }

    [Fact]
    public void MockBehaviorDefault_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.MockBehaviorDefault);
    }

    [Fact]
    public void It_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.It);
    }

    [Fact]
    public void Times_WithoutMoqReference_ReturnsNull()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.Null(symbols.Times);
    }

    [Fact]
    public void MockAs_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.MockAs.IsEmpty);
    }

    [Fact]
    public void MockOf_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.MockOf.IsEmpty);
    }

    [Fact]
    public void MockGet_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.MockGet.IsEmpty);
    }

    [Fact]
    public void Mock1As_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1As.IsEmpty);
    }

    [Fact]
    public void Mock1Setup_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1Setup.IsEmpty);
    }

    [Fact]
    public void Mock1SetupAdd_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1SetupAdd.IsEmpty);
    }

    [Fact]
    public void Mock1SetupRemove_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1SetupRemove.IsEmpty);
    }

    [Fact]
    public void Mock1SetupSequence_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1SetupSequence.IsEmpty);
    }

    [Fact]
    public void Mock1Raise_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1Raise.IsEmpty);
    }

    [Fact]
    public void Mock1Verify_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1Verify.IsEmpty);
    }

    [Fact]
    public void Mock1VerifyGet_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1VerifyGet.IsEmpty);
    }

    [Fact]
    public void Mock1VerifySet_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1VerifySet.IsEmpty);
    }

    [Fact]
    public void Mock1VerifyNoOtherCalls_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.Mock1VerifyNoOtherCalls.IsEmpty);
    }

    [Fact]
    public void MockRepositoryCreate_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.MockRepositoryCreate.IsEmpty);
    }

    [Fact]
    public void MockRepositoryVerify_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.MockRepositoryVerify.IsEmpty);
    }

    [Fact]
    public void ItIsAny_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.ItIsAny.IsEmpty);
    }

    [Fact]
    public void TimesAtLeastOnce_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.TimesAtLeastOnce.IsEmpty);
    }

    [Fact]
    public void TimesNever_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.TimesNever.IsEmpty);
    }

    [Fact]
    public void TimesOnce_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.TimesOnce.IsEmpty);
    }

    [Fact]
    public void TimesExactly_WithoutMoqReference_ReturnsEmpty()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();
        Assert.True(symbols.TimesExactly.IsEmpty);
    }

    [Fact]
    public async Task Mock_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.Mock);
        Assert.Equal("Mock", symbols.Mock!.Name);
        Assert.Equal("Moq", symbols.Mock.ContainingNamespace.Name);
    }

    [Fact]
    public async Task Mock1_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.Mock1);
        Assert.Equal("Mock", symbols.Mock1!.Name);
        Assert.Equal(1, symbols.Mock1.Arity);
    }

    [Fact]
    public async Task MockRepository_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.MockRepository);
        Assert.Equal("MockRepository", symbols.MockRepository!.Name);
    }

    [Fact]
    public async Task MockBehavior_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.MockBehavior);
        Assert.Equal(TypeKind.Enum, symbols.MockBehavior!.TypeKind);
    }

    [Fact]
    public async Task MockBehaviorStrict_WithMoqReference_ReturnsFieldSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.MockBehaviorStrict);
        Assert.Equal("Strict", symbols.MockBehaviorStrict!.Name);
    }

    [Fact]
    public async Task MockBehaviorLoose_WithMoqReference_ReturnsFieldSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.MockBehaviorLoose);
        Assert.Equal("Loose", symbols.MockBehaviorLoose!.Name);
    }

    [Fact]
    public async Task MockBehaviorDefault_WithMoqReference_ReturnsFieldSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.MockBehaviorDefault);
        Assert.Equal("Default", symbols.MockBehaviorDefault!.Name);
    }

    [Fact]
    public async Task It_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.It);
        Assert.Equal("It", symbols.It!.Name);
    }

    [Fact]
    public async Task ItIsAny_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.ItIsAny.IsEmpty);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        Assert.All(symbols.ItIsAny, m => Assert.Equal("IsAny", m.Name));
#pragma warning restore ECS0900
    }

    [Fact]
    public async Task Times_WithMoqReference_ReturnsNamedTypeSymbol()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.NotNull(symbols.Times);
        Assert.Equal("Times", symbols.Times!.Name);
    }

    [Fact]
    public async Task TimesAtLeastOnce_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.TimesAtLeastOnce.IsEmpty);
    }

    [Fact]
    public async Task TimesNever_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.TimesNever.IsEmpty);
    }

    [Fact]
    public async Task TimesOnce_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.TimesOnce.IsEmpty);
    }

    [Fact]
    public async Task TimesExactly_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.TimesExactly.IsEmpty);
    }

    [Fact]
    public async Task MockAs_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.MockAs.IsEmpty);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        Assert.All(symbols.MockAs, m => Assert.Equal("As", m.Name));
#pragma warning restore ECS0900
    }

    [Fact]
    public async Task MockOf_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.MockOf.IsEmpty);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        Assert.All(symbols.MockOf, m => Assert.Equal("Of", m.Name));
#pragma warning restore ECS0900
    }

    [Fact]
    public async Task MockGet_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.MockGet.IsEmpty);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        Assert.All(symbols.MockGet, m => Assert.Equal("Get", m.Name));
#pragma warning restore ECS0900
    }

    [Fact]
    public async Task Mock1Setup_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1Setup.IsEmpty);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        Assert.All(symbols.Mock1Setup, m => Assert.Equal("Setup", m.Name));
#pragma warning restore ECS0900
    }

    [Fact]
    public async Task Mock1SetupAdd_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1SetupAdd.IsEmpty);
    }

    [Fact]
    public async Task Mock1SetupRemove_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1SetupRemove.IsEmpty);
    }

    [Fact]
    public async Task Mock1SetupSequence_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1SetupSequence.IsEmpty);
    }

    [Fact]
    public async Task Mock1Raise_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1Raise.IsEmpty);
    }

    [Fact]
    public async Task Mock1Verify_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1Verify.IsEmpty);
    }

    [Fact]
    public async Task Mock1VerifyGet_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1VerifyGet.IsEmpty);
    }

    [Fact]
    public async Task Mock1VerifySet_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1VerifySet.IsEmpty);
    }

    [Fact]
    public async Task Mock1VerifyNoOtherCalls_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1VerifyNoOtherCalls.IsEmpty);
    }

    [Fact]
    public async Task Mock1As_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1As.IsEmpty);
    }

    [Fact]
    public async Task MockRepositoryCreate_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.MockRepositoryCreate.IsEmpty);
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        Assert.All(symbols.MockRepositoryCreate, m => Assert.Equal("Create", m.Name));
#pragma warning restore ECS0900
    }

    [Fact]
    public async Task MockRepositoryVerify_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.MockRepositoryVerify.IsEmpty);
    }

    [Fact]
    public async Task Mock1SetupGet_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1SetupGet.IsEmpty);
    }

    [Fact]
    public async Task Mock1SetupSet_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1SetupSet.IsEmpty);
    }

    [Fact]
    public async Task Mock1SetupProperty_WithMoqReference_ReturnsNonEmpty()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();
        Assert.False(symbols.Mock1SetupProperty.IsEmpty);
    }
}
