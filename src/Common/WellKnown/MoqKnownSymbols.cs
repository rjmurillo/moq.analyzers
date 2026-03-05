using Analyzer.Utilities;

namespace Moq.Analyzers.Common.WellKnown;

#pragma warning disable CS8618 // Non-nullable fields are initialized in InitializeLazyFields() called from both constructors.
internal class MoqKnownSymbols : KnownSymbols
{
    private Lazy<ImmutableArray<IMethodSymbol>> _mockAs;
    private Lazy<ImmutableArray<IMethodSymbol>> _mockOf;
    private Lazy<ImmutableArray<IMethodSymbol>> _mockGet;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1As;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1Setup;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupGet;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupSet;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupProperty;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupAdd;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupRemove;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupSequence;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1Raise;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1Verify;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1VerifyGet;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1VerifySet;
    private Lazy<ImmutableArray<IMethodSymbol>> _mock1VerifyNoOtherCalls;
    private Lazy<ImmutableArray<IMethodSymbol>> _mockRepositoryCreate;
    private Lazy<ImmutableArray<IMethodSymbol>> _mockRepositoryVerify;
    private Lazy<IFieldSymbol?> _mockBehaviorStrict;
    private Lazy<IFieldSymbol?> _mockBehaviorLoose;
    private Lazy<IFieldSymbol?> _mockBehaviorDefault;
    private Lazy<ImmutableArray<IMethodSymbol>> _iReturnsReturns;
    private Lazy<ImmutableArray<IMethodSymbol>> _iReturns1Returns;
    private Lazy<ImmutableArray<IMethodSymbol>> _iReturns2Returns;
    private Lazy<ImmutableArray<IMethodSymbol>> _iThrowsThrows;
    private Lazy<ImmutableArray<IMethodSymbol>> _returnsExtensionsReturnsAsync;
    private Lazy<ImmutableArray<IMethodSymbol>> _returnsExtensionsThrowsAsync;
    private Lazy<ImmutableArray<IMethodSymbol>> _iCallbackCallback;
    private Lazy<ImmutableArray<IMethodSymbol>> _iCallback1Callback;
    private Lazy<ImmutableArray<IMethodSymbol>> _iCallback2Callback;
    private Lazy<ImmutableArray<IMethodSymbol>> _iCallbackRaises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iCallback1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iCallback2Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iSetupGetterRaises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iSetupSetterRaises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iReturnsRaises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iReturns1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iReturns2Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iSetup1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iSetupPhrase1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iSetupGetter1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iSetupSetter1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _voidSetupPhrase1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _nonVoidSetupPhrase2Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _itIsAny;
    private Lazy<ImmutableArray<IMethodSymbol>> _iRaiseableRaises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iRaiseableAsyncRaisesAsync;
    private Lazy<ImmutableArray<IMethodSymbol>> _iRaise1Raises;
    private Lazy<ImmutableArray<IMethodSymbol>> _iRaise1RaisesAsync;
    private Lazy<ImmutableArray<IMethodSymbol>> _timesAtLeastOnce;
    private Lazy<ImmutableArray<IMethodSymbol>> _timesNever;
    private Lazy<ImmutableArray<IMethodSymbol>> _timesOnce;
    private Lazy<ImmutableArray<IMethodSymbol>> _timesExactly;

    internal MoqKnownSymbols(WellKnownTypeProvider typeProvider)
        : base(typeProvider)
    {
        InitializeLazyFields();
    }

    internal MoqKnownSymbols(Compilation compilation)
        : base(compilation)
    {
        InitializeLazyFields();
    }

#pragma warning restore CS8618

    /// <summary>
    /// Gets the class <c>Moq.Mock</c>.
    /// </summary>
    internal INamedTypeSymbol? Mock => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock");

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.As</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> MockAs => _mockAs.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.Of</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> MockOf => _mockOf.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.Get</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> MockGet => _mockGet.Value;

    /// <summary>
    /// Gets the class <c>Moq.Mock{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? Mock1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock`1");

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.As</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1As => _mock1As.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.Setup</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1Setup => _mock1Setup.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupGet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupGet => _mock1SetupGet.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupSet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupSet => _mock1SetupSet.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupProperty</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupProperty => _mock1SetupProperty.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupAdd</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupAdd => _mock1SetupAdd.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupRemove</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupRemove => _mock1SetupRemove.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupSequence</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupSequence => _mock1SetupSequence.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.Raise</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1Raise => _mock1Raise.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.Verify</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1Verify => _mock1Verify.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.VerifyGet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1VerifyGet => _mock1VerifyGet.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.VerifySet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1VerifySet => _mock1VerifySet.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.VerifyNoOtherCalls</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1VerifyNoOtherCalls => _mock1VerifyNoOtherCalls.Value;

    /// <summary>
    /// Gets the class <c>Moq.MockRepository</c>.
    /// </summary>
    internal INamedTypeSymbol? MockRepository => TypeProvider.GetOrCreateTypeByMetadataName("Moq.MockRepository");

    /// <summary>
    /// Gets the methods for <c>Moq.MockRepository.Of</c>.
    /// </summary>
    /// <remarks>
    /// <c>MockRepository</c> is a subclass of <c>MockFactory</c>.
    /// However, MockFactory is marked as obsolete. To avoid coupling
    /// ourselves to this implementation detail, we walk base types
    /// when looking for members.
    /// </remarks>
    internal ImmutableArray<IMethodSymbol> MockRepositoryCreate => _mockRepositoryCreate.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.MockRepository.Verify</c>.
    /// </summary>
    /// <remarks>
    /// <c>MockRepository</c> is a subclass of <c>MockFactory</c>.
    /// However, MockFactory is marked as obsolete. To avoid coupling
    /// ourselves to this implementation detail, we walk base types
    /// when looking for members.
    /// </remarks>
    internal ImmutableArray<IMethodSymbol> MockRepositoryVerify => _mockRepositoryVerify.Value;

    /// <summary>
    /// Gets the enum <c>Moq.MockBehavior</c>.
    /// </summary>
    internal INamedTypeSymbol? MockBehavior => TypeProvider.GetOrCreateTypeByMetadataName("Moq.MockBehavior");

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Strict</c>.
    /// </summary>
    internal IFieldSymbol? MockBehaviorStrict => _mockBehaviorStrict.Value;

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Loose</c>.
    /// </summary>
    internal IFieldSymbol? MockBehaviorLoose => _mockBehaviorLoose.Value;

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Default</c>.
    /// </summary>
    internal IFieldSymbol? MockBehaviorDefault => _mockBehaviorDefault.Value;

    /// <summary>
    /// Gets the interface <c>Moq.Language.IReturns</c>.
    /// </summary>
    internal INamedTypeSymbol? IReturns => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IReturns");

    /// <summary>
    /// Gets the interface <c>Moq.Language.IReturns{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? IReturns1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IReturns`1");

    /// <summary>
    /// Gets the interface <c>Moq.Language.IReturns{TMock, TResult}</c>.
    /// </summary>
    internal INamedTypeSymbol? IReturns2 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IReturns`2");

    /// <summary>
    /// Gets the interface <c>Moq.Language.ICallback</c>.
    /// </summary>
    internal INamedTypeSymbol? ICallback => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.ICallback");

    /// <summary>
    /// Gets the interface <c>Moq.Language.ICallback{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? ICallback1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.ICallback`1");

    /// <summary>
    /// Gets the interface <c>Moq.Language.ICallback{TMock, TResult}</c>.
    /// </summary>
    internal INamedTypeSymbol? ICallback2 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.ICallback`2");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns.Returns</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturnsReturns => _iReturnsReturns.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{T}.Returns</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns1Returns => _iReturns1Returns.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{TMock, TResult}.Returns</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns2Returns => _iReturns2Returns.Value;

    /// <summary>
    /// Gets the interface <c>Moq.Language.IThrows</c>.
    /// </summary>
    internal INamedTypeSymbol? IThrows => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IThrows");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IThrows.Throws</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IThrowsThrows => _iThrowsThrows.Value;

    /// <summary>
    /// Gets the class <c>Moq.ReturnsExtensions</c>.
    /// </summary>
    internal INamedTypeSymbol? ReturnsExtensions => TypeProvider.GetOrCreateTypeByMetadataName("Moq.ReturnsExtensions");

    /// <summary>
    /// Gets the methods for <c>Moq.ReturnsExtensions.ReturnsAsync</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ReturnsExtensionsReturnsAsync => _returnsExtensionsReturnsAsync.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.ReturnsExtensions.ThrowsAsync</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ReturnsExtensionsThrowsAsync => _returnsExtensionsThrowsAsync.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback.Callback</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallbackCallback => _iCallbackCallback.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{T}.Callback</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback1Callback => _iCallback1Callback.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{TMock, TResult}.Callback</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback2Callback => _iCallback2Callback.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallbackRaises => _iCallbackRaises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback1Raises => _iCallback1Raises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{TMock, TResult}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback2Raises => _iCallback2Raises.Value;

    /// <summary>
    /// Gets the interface <c>Moq.Language.ISetupGetter{TMock, TProperty}</c>.
    /// </summary>
    internal INamedTypeSymbol? ISetupGetter => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.ISetupGetter`2");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ISetupGetter{TMock, TProperty}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ISetupGetterRaises => _iSetupGetterRaises.Value;

    /// <summary>
    /// Gets the interface <c>Moq.Language.ISetupSetter{TMock, TProperty}</c>.
    /// </summary>
    internal INamedTypeSymbol? ISetupSetter => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.ISetupSetter`2");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ISetupSetter{TMock, TProperty}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ISetupSetterRaises => _iSetupSetterRaises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturnsRaises => _iReturnsRaises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns1Raises => _iReturns1Raises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{TMock, TResult}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns2Raises => _iReturns2Raises.Value;

    /// <summary>
    /// Gets the interface <c>Moq.Language.Flow.ISetup{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? ISetup1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.ISetup`1");

    /// <summary>
    /// Gets the interface <c>Moq.Language.Flow.ISetupPhrase{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? ISetupPhrase1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.ISetupPhrase`1");

    /// <summary>
    /// Gets the interface <c>Moq.Language.Flow.ISetupGetter{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? ISetupGetter1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.ISetupGetter`1");

    /// <summary>
    /// Gets the interface <c>Moq.Language.Flow.ISetupSetter{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? ISetupSetter1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.ISetupSetter`1");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.Flow.ISetup{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ISetup1Raises => _iSetup1Raises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.Flow.ISetupPhrase{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ISetupPhrase1Raises => _iSetupPhrase1Raises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.Flow.ISetupGetter{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ISetupGetter1Raises => _iSetupGetter1Raises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.Flow.ISetupSetter{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ISetupSetter1Raises => _iSetupSetter1Raises.Value;

    /// <summary>
    /// Gets the concrete fluent setup phrase class <c>Moq.Language.Flow.VoidSetupPhrase{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? VoidSetupPhrase1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.VoidSetupPhrase`1");

    /// <summary>
    /// Gets the concrete fluent setup phrase class <c>Moq.Language.Flow.NonVoidSetupPhrase{T,TResult}</c>.
    /// </summary>
    internal INamedTypeSymbol? NonVoidSetupPhrase2 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.NonVoidSetupPhrase`2");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.Flow.VoidSetupPhrase{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> VoidSetupPhrase1Raises => _voidSetupPhrase1Raises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.Flow.NonVoidSetupPhrase{T,TResult}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> NonVoidSetupPhrase2Raises => _nonVoidSetupPhrase2Raises.Value;

    /// <summary>
    /// Gets the class <c>Moq.It</c>.
    /// </summary>
    internal INamedTypeSymbol? It => TypeProvider.GetOrCreateTypeByMetadataName("Moq.It");

    /// <summary>
    /// Gets the methods for <c>Moq.It.IsAny</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ItIsAny => _itIsAny.Value;

    /// <summary>
    /// Gets the interface <c>Moq.Language.IRaiseable</c>.
    /// </summary>
    internal INamedTypeSymbol? IRaiseable => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IRaiseable");

    /// <summary>
    /// Gets the interface <c>Moq.Language.IRaiseableAsync</c>.
    /// </summary>
    internal INamedTypeSymbol? IRaiseableAsync => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IRaiseableAsync");

    /// <summary>
    /// Gets the interface <c>Moq.Language.IRaise{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? IRaise1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IRaise`1");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IRaiseable.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IRaiseableRaises => _iRaiseableRaises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IRaiseableAsync.RaisesAsync</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IRaiseableAsyncRaisesAsync => _iRaiseableAsyncRaisesAsync.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IRaise{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IRaise1Raises => _iRaise1Raises.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IRaise{T}.RaisesAsync</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IRaise1RaisesAsync => _iRaise1RaisesAsync.Value;

    /// <summary>
    /// Gets the struct <c>Moq.Times</c>.
    /// </summary>
    internal INamedTypeSymbol? Times => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Times");

    /// <summary>
    /// Gets the methods for <c>Moq.Times.AtLeastOnce</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesAtLeastOnce => _timesAtLeastOnce.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Times.Never</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesNever => _timesNever.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Times.Once</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesOnce => _timesOnce.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Times.Exactly</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesExactly => _timesExactly.Value;

    /// <summary>
    /// Gets the interface <c>Microsoft.Extensions.Logging.ILogger</c>.
    /// </summary>
    internal INamedTypeSymbol? ILogger => TypeProvider.GetOrCreateTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");

    /// <summary>
    /// Gets the interface <c>Microsoft.Extensions.Logging.ILogger{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? ILogger1 => TypeProvider.GetOrCreateTypeByMetadataName("Microsoft.Extensions.Logging.ILogger`1");

    /// <summary>
    /// Initializes all lazy backing fields. Called from both constructors to ensure
    /// member lookups are computed at most once per instance, eliminating per-access
    /// allocations from GetMembers().OfType{T}().ToImmutableArray().
    /// </summary>
    private void InitializeLazyFields()
    {
        InitializeMockFields();
        InitializeMockRepositoryAndBehaviorFields();
        InitializeReturnsAndCallbackFields();
        InitializeSetupRaisesFields();
        InitializeRaiseAndTimesFields();
    }

    private void InitializeMockFields()
    {
        _mockAs = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock?.GetMembers("As").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mockOf = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock?.GetMembers("Of").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mockGet = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock?.GetMembers("Get").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1As = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("As").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1Setup = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("Setup").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1SetupGet = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("SetupGet").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1SetupSet = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("SetupSet").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1SetupProperty = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("SetupProperty").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1SetupAdd = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("SetupAdd").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1SetupRemove = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("SetupRemove").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1SetupSequence = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("SetupSequence").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1Raise = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("Raise").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1Verify = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("Verify").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1VerifyGet = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("VerifyGet").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1VerifySet = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("VerifySet").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mock1VerifyNoOtherCalls = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Mock1?.GetMembers("VerifyNoOtherCalls").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
    }

    private void InitializeMockRepositoryAndBehaviorFields()
    {
#pragma warning disable ECS0900 // Boxing from SelectMany over GetBaseTypesAndThis(); acceptable for lazy-evaluated one-time initialization.
        _mockRepositoryCreate = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            MockRepository?.GetBaseTypesAndThis().SelectMany(type => type.GetMembers("Create")).OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _mockRepositoryVerify = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            MockRepository?.GetBaseTypesAndThis().SelectMany(type => type.GetMembers("Verify")).OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
#pragma warning restore ECS0900
        _mockBehaviorStrict = new Lazy<IFieldSymbol?>(() =>
            MockBehavior?.GetMembers("Strict").OfType<IFieldSymbol>().SingleOrDefault());
        _mockBehaviorLoose = new Lazy<IFieldSymbol?>(() =>
            MockBehavior?.GetMembers("Loose").OfType<IFieldSymbol>().SingleOrDefault());
        _mockBehaviorDefault = new Lazy<IFieldSymbol?>(() =>
            MockBehavior?.GetMembers("Default").OfType<IFieldSymbol>().SingleOrDefault());
        _itIsAny = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            It?.GetMembers("IsAny").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
    }

    private void InitializeReturnsAndCallbackFields()
    {
        _iReturnsReturns = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IReturns?.GetMembers("Returns").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iReturns1Returns = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IReturns1?.GetMembers("Returns").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iReturns2Returns = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IReturns2?.GetMembers("Returns").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iThrowsThrows = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IThrows?.GetMembers("Throws").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _returnsExtensionsReturnsAsync = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ReturnsExtensions?.GetMembers("ReturnsAsync").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _returnsExtensionsThrowsAsync = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ReturnsExtensions?.GetMembers("ThrowsAsync").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iCallbackCallback = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ICallback?.GetMembers("Callback").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iCallback1Callback = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ICallback1?.GetMembers("Callback").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iCallback2Callback = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ICallback2?.GetMembers("Callback").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iCallbackRaises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ICallback?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iCallback1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ICallback1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iCallback2Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ICallback2?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
    }

    private void InitializeSetupRaisesFields()
    {
        _iSetupGetterRaises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ISetupGetter?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iSetupSetterRaises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ISetupSetter?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iReturnsRaises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IReturns?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iReturns1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IReturns1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iReturns2Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IReturns2?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iSetup1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ISetup1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iSetupPhrase1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ISetupPhrase1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iSetupGetter1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ISetupGetter1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iSetupSetter1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            ISetupSetter1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _voidSetupPhrase1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            VoidSetupPhrase1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _nonVoidSetupPhrase2Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            NonVoidSetupPhrase2?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
    }

    private void InitializeRaiseAndTimesFields()
    {
        _iRaiseableRaises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IRaiseable?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iRaiseableAsyncRaisesAsync = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IRaiseableAsync?.GetMembers("RaisesAsync").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iRaise1Raises = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IRaise1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _iRaise1RaisesAsync = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            IRaise1?.GetMembers("RaisesAsync").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _timesAtLeastOnce = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Times?.GetMembers("AtLeastOnce").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _timesNever = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Times?.GetMembers("Never").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _timesOnce = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Times?.GetMembers("Once").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
        _timesExactly = new Lazy<ImmutableArray<IMethodSymbol>>(() =>
            Times?.GetMembers("Exactly").OfType<IMethodSymbol>().ToImmutableArray()
            ?? ImmutableArray<IMethodSymbol>.Empty);
    }
}
