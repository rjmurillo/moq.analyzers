using Analyzer.Utilities;

namespace Moq.Analyzers.Common.WellKnown;

internal class MoqKnownSymbols : KnownSymbols
{
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mockAs;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mockOf;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mockGet;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1As;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1Setup;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupGet;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupSet;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupProperty;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupAdd;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupRemove;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1SetupSequence;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1Raise;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1Verify;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1VerifyGet;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1VerifySet;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mock1VerifyNoOtherCalls;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mockRepositoryCreate;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _mockRepositoryVerify;
    private readonly Lazy<IFieldSymbol?> _mockBehaviorStrict;
    private readonly Lazy<IFieldSymbol?> _mockBehaviorLoose;
    private readonly Lazy<IFieldSymbol?> _mockBehaviorDefault;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iReturnsReturns;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iReturns1Returns;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iReturns2Returns;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iThrowsThrows;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _returnsExtensionsReturnsAsync;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _returnsExtensionsThrowsAsync;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iCallbackCallback;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iCallback1Callback;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iCallback2Callback;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iCallbackRaises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iCallback1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iCallback2Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iSetupGetterRaises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iSetupSetterRaises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iReturnsRaises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iReturns1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iReturns2Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iSetup1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iSetupPhrase1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iSetupGetter1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iSetupSetter1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _voidSetupPhrase1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _nonVoidSetupPhrase2Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iProtectedMock1Setup;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iProtectedMock1SetupSet;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iProtectedMock1SetupSequence;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iProtectedMock1Verify;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iProtectedMock1VerifySet;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _itIsAny;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iRaiseableRaises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iRaiseableAsyncRaisesAsync;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iRaise1Raises;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _iRaise1RaisesAsync;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _timesAtLeastOnce;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _timesNever;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _timesOnce;
    private readonly Lazy<ImmutableArray<IMethodSymbol>> _timesExactly;

#pragma warning disable MA0051 // Constructor length is proportional to the number of Moq symbols cached; each line is a single-field assignment.
    internal MoqKnownSymbols(WellKnownTypeProvider typeProvider)
        : base(typeProvider)
    {
        // Mock and Mock<T> members
        _mockAs = CreateLazyMethods(Mock, "As");
        _mockOf = CreateLazyMethods(Mock, "Of");
        _mockGet = CreateLazyMethods(Mock, "Get");
        _mock1As = CreateLazyMethods(Mock1, "As");
        _mock1Setup = CreateLazyMethods(Mock1, "Setup");
        _mock1SetupGet = CreateLazyMethods(Mock1, "SetupGet");
        _mock1SetupSet = CreateLazyMethods(Mock1, "SetupSet");
        _mock1SetupProperty = CreateLazyMethods(Mock1, "SetupProperty");
        _mock1SetupAdd = CreateLazyMethods(Mock1, "SetupAdd");
        _mock1SetupRemove = CreateLazyMethods(Mock1, "SetupRemove");
        _mock1SetupSequence = CreateLazyMethods(Mock1, "SetupSequence");
        _mock1Raise = CreateLazyMethods(Mock1, "Raise");
        _mock1Verify = CreateLazyMethods(Mock1, "Verify");
        _mock1VerifyGet = CreateLazyMethods(Mock1, "VerifyGet");
        _mock1VerifySet = CreateLazyMethods(Mock1, "VerifySet");
        _mock1VerifyNoOtherCalls = CreateLazyMethods(Mock1, "VerifyNoOtherCalls");

        // MockRepository members (walks base types for inherited members)
        _mockRepositoryCreate = CreateLazyInheritedMethods(MockRepository, "Create");
        _mockRepositoryVerify = CreateLazyInheritedMethods(MockRepository, "Verify");

        // MockBehavior enum fields
        _mockBehaviorStrict = CreateLazySingleField(MockBehavior, "Strict");
        _mockBehaviorLoose = CreateLazySingleField(MockBehavior, "Loose");
        _mockBehaviorDefault = CreateLazySingleField(MockBehavior, "Default");

        // IProtectedMock members (only those used by analyzers)
        _iProtectedMock1Setup = CreateLazyMethods(IProtectedMock1, "Setup");
        _iProtectedMock1SetupSet = CreateLazyMethods(IProtectedMock1, "SetupSet");
        _iProtectedMock1SetupSequence = CreateLazyMethods(IProtectedMock1, "SetupSequence");
        _iProtectedMock1Verify = CreateLazyMethods(IProtectedMock1, "Verify");
        _iProtectedMock1VerifySet = CreateLazyMethods(IProtectedMock1, "VerifySet");

        // It members
        _itIsAny = CreateLazyMethods(It, "IsAny");

        // IReturns, IThrows, ReturnsExtensions, and ICallback members
        _iReturnsReturns = CreateLazyMethods(IReturns, "Returns");
        _iReturns1Returns = CreateLazyMethods(IReturns1, "Returns");
        _iReturns2Returns = CreateLazyMethods(IReturns2, "Returns");
        _iThrowsThrows = CreateLazyMethods(IThrows, "Throws");
        _returnsExtensionsReturnsAsync = CreateLazyMethods(ReturnsExtensions, "ReturnsAsync");
        _returnsExtensionsThrowsAsync = CreateLazyMethods(ReturnsExtensions, "ThrowsAsync");
        _iCallbackCallback = CreateLazyMethods(ICallback, "Callback");
        _iCallback1Callback = CreateLazyMethods(ICallback1, "Callback");
        _iCallback2Callback = CreateLazyMethods(ICallback2, "Callback");
        _iCallbackRaises = CreateLazyMethods(ICallback, "Raises");
        _iCallback1Raises = CreateLazyMethods(ICallback1, "Raises");
        _iCallback2Raises = CreateLazyMethods(ICallback2, "Raises");

        // Setup Raises members
        _iSetupGetterRaises = CreateLazyMethods(ISetupGetter, "Raises");
        _iSetupSetterRaises = CreateLazyMethods(ISetupSetter, "Raises");
        _iReturnsRaises = CreateLazyMethods(IReturns, "Raises");
        _iReturns1Raises = CreateLazyMethods(IReturns1, "Raises");
        _iReturns2Raises = CreateLazyMethods(IReturns2, "Raises");
        _iSetup1Raises = CreateLazyMethods(ISetup1, "Raises");
        _iSetupPhrase1Raises = CreateLazyMethods(ISetupPhrase1, "Raises");
        _iSetupGetter1Raises = CreateLazyMethods(ISetupGetter1, "Raises");
        _iSetupSetter1Raises = CreateLazyMethods(ISetupSetter1, "Raises");
        _voidSetupPhrase1Raises = CreateLazyMethods(VoidSetupPhrase1, "Raises");
        _nonVoidSetupPhrase2Raises = CreateLazyMethods(NonVoidSetupPhrase2, "Raises");

        // IRaiseable and IRaise members
        _iRaiseableRaises = CreateLazyMethods(IRaiseable, "Raises");
        _iRaiseableAsyncRaisesAsync = CreateLazyMethods(IRaiseableAsync, "RaisesAsync");
        _iRaise1Raises = CreateLazyMethods(IRaise1, "Raises");
        _iRaise1RaisesAsync = CreateLazyMethods(IRaise1, "RaisesAsync");

        // Times members
        _timesAtLeastOnce = CreateLazyMethods(Times, "AtLeastOnce");
        _timesNever = CreateLazyMethods(Times, "Never");
        _timesOnce = CreateLazyMethods(Times, "Once");
        _timesExactly = CreateLazyMethods(Times, "Exactly");
    }
#pragma warning restore MA0051

    internal MoqKnownSymbols(Compilation compilation)
        : this(WellKnownTypeProvider.GetOrCreate(compilation))
    {
    }

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
    /// Gets the interface <c>Moq.Language.Flow.IReturnsResult{TMock}</c>.
    /// This is the actual return type of <c>.Returns()</c> and <c>.ReturnsAsync()</c>.
    /// </summary>
    internal INamedTypeSymbol? IReturnsResult1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.IReturnsResult`1");

    /// <summary>
    /// Gets the interface <c>Moq.Language.Flow.IThrowsResult</c>.
    /// This is the return type of <c>.Throws()</c>.
    /// </summary>
    internal INamedTypeSymbol? IThrowsResult => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.Flow.IThrowsResult");

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
    /// Gets the interface <c>Moq.Protected.IProtectedMock{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? IProtectedMock1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Protected.IProtectedMock`1");

    /// <summary>
    /// Gets the methods for <c>Moq.Protected.IProtectedMock{T}.Setup</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IProtectedMock1Setup => _iProtectedMock1Setup.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Protected.IProtectedMock{T}.SetupSet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IProtectedMock1SetupSet => _iProtectedMock1SetupSet.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Protected.IProtectedMock{T}.SetupSequence</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IProtectedMock1SetupSequence => _iProtectedMock1SetupSequence.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Protected.IProtectedMock{T}.Verify</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IProtectedMock1Verify => _iProtectedMock1Verify.Value;

    /// <summary>
    /// Gets the methods for <c>Moq.Protected.IProtectedMock{T}.VerifySet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IProtectedMock1VerifySet => _iProtectedMock1VerifySet.Value;

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
    /// Creates a <see cref="Lazy{T}"/> that resolves all methods with <paramref name="memberName"/> declared directly on <paramref name="type"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> ensures the value is computed once
    /// even when multiple analyzer threads access it concurrently.
    /// </remarks>
    private static Lazy<ImmutableArray<IMethodSymbol>> CreateLazyMethods(INamedTypeSymbol? type, string memberName) =>
        new Lazy<ImmutableArray<IMethodSymbol>>(
            () => type?.GetMembers(memberName).OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty,
            LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Creates a <see cref="Lazy{T}"/> that resolves all methods with <paramref name="memberName"/> from <paramref name="type"/> and its base types.
    /// </summary>
    /// <remarks>
    /// Used for types like <see cref="MockRepository"/> that inherit members from an obsolete base class.
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> ensures the value is computed once
    /// even when multiple analyzer threads access it concurrently.
    /// </remarks>
#pragma warning disable ECS0900 // Minimize boxing and unboxing: SelectMany over GetBaseTypesAndThis() boxes; acceptable for lazy-evaluated one-time initialization.
    private static Lazy<ImmutableArray<IMethodSymbol>> CreateLazyInheritedMethods(INamedTypeSymbol? type, string memberName) =>
        new Lazy<ImmutableArray<IMethodSymbol>>(
            () => type?.GetBaseTypesAndThis().SelectMany(t => t.GetMembers(memberName)).OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty,
            LazyThreadSafetyMode.ExecutionAndPublication);
#pragma warning restore ECS0900

    /// <summary>
    /// Creates a <see cref="Lazy{T}"/> that resolves a single field with <paramref name="memberName"/> declared on <paramref name="type"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> ensures the value is computed once
    /// even when multiple analyzer threads access it concurrently.
    /// </remarks>
    private static Lazy<IFieldSymbol?> CreateLazySingleField(INamedTypeSymbol? type, string memberName) =>
        new Lazy<IFieldSymbol?>(
            () => type?.GetMembers(memberName).OfType<IFieldSymbol>().SingleOrDefault(),
            LazyThreadSafetyMode.ExecutionAndPublication);
}
