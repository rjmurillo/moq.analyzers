namespace Moq.Analyzers.Common;

internal static class WellKnownMoqNames
{
    internal static readonly string MoqNamespace = "Moq";

    internal static readonly string MoqSymbolName = "Moq";
    internal static readonly string MockTypeName = "Mock";
    internal static readonly string MockBehaviorTypeName = "MockBehavior";
    internal static readonly string MockFactoryTypeName = "MockFactory";

    internal static readonly string FullyQualifiedMoqMockTypeName = $"{MoqNamespace}.{MockTypeName}";
    internal static readonly string FullyQualifiedMoqMock1TypeName = $"{FullyQualifiedMoqMockTypeName}`1";
    internal static readonly string FullyQualifiedMoqBehaviorTypeName = $"{MoqNamespace}.{MockBehaviorTypeName}";
    internal static readonly string FullyQualifiedMoqRepositoryTypeName = $"{MoqNamespace}.MockRepository";

    internal static readonly string AsMethodName = "As";
    internal static readonly string CreateMethodName = "Create";
    internal static readonly string OfMethodName = "Of";
}
