namespace Moq.Analyzers.Common;

/// <summary>
/// Provides well-known names and fully qualified names for commonly used Moq types, namespaces, and members.
/// </summary>
internal static class WellKnownMoqNames
{
    /// <summary>
    /// Represents the namespace for the Moq library.
    /// </summary>
    internal static readonly string MoqNamespace = "Moq";

    /// <summary>
    /// The name of the 'Moq.Mock' type.
    /// </summary>
    internal static readonly string MockTypeName = "Mock";

    /// <summary>
    /// The name of the 'Moq.MockBehavior' type.
    /// This type specifies the behavior of the mock (Strict, Loose, etc.).
    /// </summary>
    internal static readonly string MockBehaviorTypeName = "MockBehavior";

    /// <summary>
    /// The name of the 'Moq.MockFactory' type.
    /// This factory is used for creating multiple mock objects with a shared configuration.
    /// </summary>
    internal static readonly string MockFactoryTypeName = "MockFactory";

    /// <summary>
    /// Fully qualified name for the 'Moq.Mock' type.
    /// </summary>
    internal static readonly string FullyQualifiedMoqMockTypeName = $"{MoqNamespace}.{MockTypeName}";

    /// <summary>
    /// Fully qualified name for the generic version of 'Moq.Mock{T}'.
    /// Represents mocks for specific types.
    /// </summary>
    internal static readonly string FullyQualifiedMoqMock1TypeName = $"{FullyQualifiedMoqMockTypeName}`1";

    /// <summary>
    /// Fully qualified name for the 'Moq.MockBehavior' type.
    /// </summary>
    internal static readonly string FullyQualifiedMoqBehaviorTypeName = $"{MoqNamespace}.{MockBehaviorTypeName}";

    /// <summary>
    /// Fully qualified name for the 'Moq.MockRepository' type.
    /// This type acts as a container for multiple mocks and shared mock configurations.
    /// </summary>
    internal static readonly string FullyQualifiedMoqRepositoryTypeName = $"{MoqNamespace}.MockRepository";

    /// <summary>
    /// Represents the method name for the `As` method in Moq.
    /// This method is used to cast mocks to interfaces.
    /// </summary>
    internal static readonly string AsMethodName = "As";

    /// <summary>
    /// Represents the method name for the `Create` method in Moq.
    /// This method is used to create instances of mocks.
    /// </summary>
    internal static readonly string CreateMethodName = "Create";

    /// <summary>
    /// Represents the method name for the `Of` method in Moq.
    /// This method is used to create a mock from a type without directly specifying the constructor.
    /// </summary>
    internal static readonly string OfMethodName = "Of";
}
