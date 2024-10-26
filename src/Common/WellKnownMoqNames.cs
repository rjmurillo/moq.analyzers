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
    /// Represents the method name for the `As` method in Moq.
    /// This method is used to cast mocks to interfaces.
    /// </summary>
    internal static readonly string AsMethodName = "As";
}
