using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;

namespace Moq.Analyzers.Common.WellKnown;

internal class MoqKnownSymbols : KnownSymbols
{
    public MoqKnownSymbols(WellKnownTypeProvider typeProvider)
        : base(typeProvider)
    {
    }

    public MoqKnownSymbols(Compilation compilation)
        : base(compilation)
    {
    }

    /// <summary>
    /// Gets the class <c>Moq.Mock</c>.
    /// </summary>
    public INamedTypeSymbol? Mock => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock");

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.As</c>.
    /// </summary>
    public ImmutableArray<IMethodSymbol> MockAs => Mock?.GetMembers("As").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock.Of</c>.
    /// </summary>
    public ImmutableArray<IMethodSymbol> MockOf => Mock?.GetMembers("Of").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the class <c>Moq.Mock&lt;T&gt;</c>.
    /// </summary>
    public INamedTypeSymbol? Mock1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock`1");

    /// <summary>
    /// Gets the methods for <c>Moq.Mock&lt;T&gt;.As</c>.
    /// </summary>
    public ImmutableArray<IMethodSymbol> Mock1As => Mock1?.GetMembers("As").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the methods for <c>Moq.Mock&lt;T&gt;.Setup</c>.
    /// </summary>
    public ImmutableArray<IMethodSymbol> Mock1Setup => Mock1?.GetMembers("Setup").OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the class <c>Moq.MockRepository</c>.
    /// </summary>
    public INamedTypeSymbol? MockRepository => TypeProvider.GetOrCreateTypeByMetadataName("Moq.MockRepository");

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
    public ImmutableArray<IMethodSymbol> MockRepositoryCreate => MockRepository?.GetBaseTypesAndThis().SelectMany(type => type.GetMembers("Create")).OfType<IMethodSymbol>().ToImmutableArray() ?? ImmutableArray<IMethodSymbol>.Empty;

    /// <summary>
    /// Gets the enum <c>Moq.MockBehavior</c>.
    /// </summary>
    public INamedTypeSymbol? MockBehavior => TypeProvider.GetOrCreateTypeByMetadataName("Moq.MockBehavior");

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Strict</c>.
    /// </summary>
    public IFieldSymbol? MockBehaviorStrict => MockBehavior?.GetMembers("Strict").OfType<IFieldSymbol>().SingleOrDefault();

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Loose</c>.
    /// </summary>
    public IFieldSymbol? MockBehaviorLoose => MockBehavior?.GetMembers("Loose").OfType<IFieldSymbol>().SingleOrDefault();

    /// <summary>
    /// Gets the field <c>Moq.MockBehavior.Default</c>.
    /// </summary>
    public IFieldSymbol? MockBehaviorDefault => MockBehavior?.GetMembers("Default").OfType<IFieldSymbol>().SingleOrDefault();
}
