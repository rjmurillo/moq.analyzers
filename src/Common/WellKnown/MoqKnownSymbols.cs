using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;

namespace Moq.Analyzers.Common.WellKnown;

internal class MoqKnownSymbols : KnownSymbols
{
    internal MoqKnownSymbols(WellKnownTypeProvider typeProvider)
        : base(typeProvider)
    {
    }

    internal MoqKnownSymbols(Compilation compilation)
        : base(compilation)
    {
    }

    /// <summary>
    /// Gets the class <c>Moq.Mock</c>.
    /// </summary>
    internal INamedTypeSymbol? Mock => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock");

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.As</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> MockAs => Mock?.GetMembers("As").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.Of</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> MockOf => Mock?.GetMembers("Of").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.Get</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> MockGet => Mock?.GetMembers("Get").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the class <c>Moq.Mock{T}</c>.
    /// </summary>
    internal INamedTypeSymbol? Mock1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock`1");

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.As</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1As => Mock1?.GetMembers("As").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.Setup</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1Setup => Mock1?.GetMembers("Setup").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupAdd</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupAdd => Mock1?.GetMembers("SetupAdd").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupRemove</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupRemove => Mock1?.GetMembers("SetupRemove").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.SetupSequence</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1SetupSequence => Mock1?.GetMembers("SetupSequence").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.Raise</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1Raise => Mock1?.GetMembers("Raise").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.Verify</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1Verify => Mock1?.GetMembers("Verify").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.VerifyGet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1VerifyGet => Mock1?.GetMembers("VerifyGet").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.VerifySet</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1VerifySet => Mock1?.GetMembers("VerifySet").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock{T}.VerifyNoOtherCalls</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> Mock1VerifyNoOtherCalls => Mock1?.GetMembers("VerifyNoOtherCalls").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

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
    [SuppressMessage("Performance", "ECS0900:Minimize boxing and unboxing", Justification = "Minor perf issues. Should revisit later.")]
    internal ImmutableArray<IMethodSymbol> MockRepositoryCreate => MockRepository?.GetBaseTypesAndThis().SelectMany(type => type.GetMembers("Create")).OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.MockRepository.Verify</c>.
    /// </summary>
    /// <remarks>
    /// <c>MockRepository</c> is a subclass of <c>MockFactory</c>.
    /// However, MockFactory is marked as obsolete. To avoid coupling
    /// ourselves to this implementation detail, we walk base types
    /// when looking for members.
    /// </remarks>
    [SuppressMessage("Performance", "ECS0900:Minimize boxing and unboxing", Justification = "Minor perf issues. Should revisit later.")]
    internal ImmutableArray<IMethodSymbol> MockRepositoryVerify => MockRepository?.GetBaseTypesAndThis().SelectMany(type => type.GetMembers("Verify")).OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the enum <c>Moq.MockBehavior</c>.
    /// </summary>
    internal INamedTypeSymbol? MockBehavior => TypeProvider.GetOrCreateTypeByMetadataName("Moq.MockBehavior");

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Strict</c>.
    /// </summary>
    internal IFieldSymbol? MockBehaviorStrict => MockBehavior?.GetMembers("Strict").OfType<IFieldSymbol>().SingleOrDefault();

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Loose</c>.
    /// </summary>
    internal IFieldSymbol? MockBehaviorLoose => MockBehavior?.GetMembers("Loose").OfType<IFieldSymbol>().SingleOrDefault();

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Default</c>.
    /// </summary>
    internal IFieldSymbol? MockBehaviorDefault => MockBehavior?.GetMembers("Default").OfType<IFieldSymbol>().SingleOrDefault();

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
    internal ImmutableArray<IMethodSymbol> IReturnsReturns => IReturns?.GetMembers("Returns").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{T}.Returns</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns1Returns => IReturns1?.GetMembers("Returns").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{TMock, TResult}.Returns</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns2Returns => IReturns2?.GetMembers("Returns").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback.Callback</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallbackCallback => ICallback?.GetMembers("Callback").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{T}.Callback</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback1Callback => ICallback1?.GetMembers("Callback").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{TMock, TResult}.Callback</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback2Callback => ICallback2?.GetMembers("Callback").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallbackRaises => ICallback?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback1Raises => ICallback1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.ICallback{TMock, TResult}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ICallback2Raises => ICallback2?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturnsRaises => IReturns?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{T}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns1Raises => IReturns1?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IReturns{TMock, TResult}.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IReturns2Raises => IReturns2?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the class <c>Moq.It</c>.
    /// </summary>
    internal INamedTypeSymbol? It => TypeProvider.GetOrCreateTypeByMetadataName("Moq.It");

    /// <summary>
    /// Gets the methods for <c>Moq.It.IsAny</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ItIsAny => It?.GetMembers("IsAny").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the interface <c>Moq.Language.IRaiseable</c>.
    /// </summary>
    internal INamedTypeSymbol? IRaiseable => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IRaiseable");

    /// <summary>
    /// Gets the interface <c>Moq.Language.IRaiseableAsync</c>.
    /// </summary>
    internal INamedTypeSymbol? IRaiseableAsync => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Language.IRaiseableAsync");

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IRaiseable.Raises</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IRaiseableRaises => IRaiseable?.GetMembers("Raises").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Language.IRaiseableAsync.RaisesAsync</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> IRaiseableAsyncRaisesAsync => IRaiseableAsync?.GetMembers("RaisesAsync").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the struct <c>Moq.Times</c>.
    /// </summary>
    internal INamedTypeSymbol? Times => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Times");

    /// <summary>
    /// Gets the methods for <c>Moq.Times.AtLeastOnce</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesAtLeastOnce => Times?.GetMembers("AtLeastOnce").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Times.Never</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesNever => Times?.GetMembers("Never").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Times.Once</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesOnce => Times?.GetMembers("Once").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Times.Exactly</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> TimesExactly => Times?.GetMembers("Exactly").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;
}
