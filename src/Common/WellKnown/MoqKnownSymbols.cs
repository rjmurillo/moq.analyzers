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
    /// Gets the class <c>Moq.It</c>.
    /// </summary>
    internal INamedTypeSymbol? It => TypeProvider.GetOrCreateTypeByMetadataName("Moq.It");

    /// <summary>
    /// Gets the methods for <c>Moq.It.IsAny</c>.
    /// </summary>
    internal ImmutableArray<IMethodSymbol> ItIsAny => It?.GetMembers("IsAny").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;
}
