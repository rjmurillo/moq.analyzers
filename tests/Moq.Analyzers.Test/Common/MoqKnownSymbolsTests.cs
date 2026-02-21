using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public class MoqKnownSymbolsTests
{
#pragma warning disable ECS1300 // Static field init is simpler than static constructor for single field
    private static readonly MetadataReference SystemThreadingTasksReference =
        MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location);
#pragma warning restore ECS1300

    private static MetadataReference[] CoreReferencesWithTasks =>
        [CompilationHelper.CorlibReference, CompilationHelper.SystemRuntimeReference, SystemThreadingTasksReference, CompilationHelper.SystemLinqReference];

    [Fact]
    public void Constructor_WithCompilation_CreatesInstance()
    {
        CSharpCompilation compilation = CreateMinimalCompilation();

        MoqKnownSymbols symbols = new MoqKnownSymbols(compilation);

        // Verify it does not throw and the object is usable.
        Assert.Null(symbols.Mock);
    }

    [Fact]
    public void Constructor_WithWellKnownTypeProvider_CreatesInstance()
    {
        CSharpCompilation compilation = CreateMinimalCompilation();
        Analyzer.Utilities.WellKnownTypeProvider typeProvider =
            Analyzer.Utilities.WellKnownTypeProvider.GetOrCreate(compilation);

        MoqKnownSymbols symbols = new MoqKnownSymbols(typeProvider);

        Assert.Null(symbols.Mock);
    }

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

    [Fact]
    public async Task BothConstructors_ProduceSameResults_ForMockType()
    {
        CSharpCompilation compilation = await CreateMoqCompilationAsync();
        Analyzer.Utilities.WellKnownTypeProvider typeProvider =
            Analyzer.Utilities.WellKnownTypeProvider.GetOrCreate(compilation);

        MoqKnownSymbols fromCompilation = new MoqKnownSymbols(compilation);
        MoqKnownSymbols fromProvider = new MoqKnownSymbols(typeProvider);

        Assert.NotNull(fromCompilation.Mock);
        Assert.NotNull(fromProvider.Mock);
        Assert.True(SymbolEqualityComparer.Default.Equals(fromCompilation.Mock, fromProvider.Mock));
    }

    [Fact]
    public async Task BothConstructors_ProduceSameResults_ForMock1Type()
    {
        CSharpCompilation compilation = await CreateMoqCompilationAsync();
        Analyzer.Utilities.WellKnownTypeProvider typeProvider =
            Analyzer.Utilities.WellKnownTypeProvider.GetOrCreate(compilation);

        MoqKnownSymbols fromCompilation = new MoqKnownSymbols(compilation);
        MoqKnownSymbols fromProvider = new MoqKnownSymbols(typeProvider);

        Assert.NotNull(fromCompilation.Mock1);
        Assert.NotNull(fromProvider.Mock1);
        Assert.True(SymbolEqualityComparer.Default.Equals(fromCompilation.Mock1, fromProvider.Mock1));
    }

    private static CSharpCompilation CreateMinimalCompilation()
    {
        (SemanticModel model, _) = CompilationHelper.CreateCompilation("public class Empty { }", CoreReferencesWithTasks);
        return (CSharpCompilation)model.Compilation;
    }

    private static MoqKnownSymbols CreateSymbolsWithoutMoq()
    {
        return new MoqKnownSymbols(CreateMinimalCompilation());
    }

    private static async Task<MoqKnownSymbols> CreateSymbolsWithMoqAsync()
    {
        return new MoqKnownSymbols(await CreateMoqCompilationAsync().ConfigureAwait(false));
    }

    private static async Task<CSharpCompilation> CreateMoqCompilationAsync()
    {
        (SemanticModel model, _) = await CompilationHelper.CreateMoqCompilationAsync("public class Empty { }").ConfigureAwait(false);
        return (CSharpCompilation)model.Compilation;
    }
}
