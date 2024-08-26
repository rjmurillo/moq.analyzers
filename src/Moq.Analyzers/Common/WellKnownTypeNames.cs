namespace Moq.Analyzers.Common;

#pragma warning disable ECS0200 // Consider using readonly instead of const for flexibility

internal static class WellKnownTypeNames
{
    internal const string Moq = nameof(Moq);
    internal const string MockName = "Mock";
    internal const string MockBehavior = nameof(MockBehavior);
    internal const string MockFactory = nameof(MockFactory);
    internal const string MoqMock = $"{Moq}.{MockName}";
    internal const string MoqMock1 = $"{MoqMock}`1";
    internal const string MoqBehavior = $"{Moq}.{MockBehavior}";
    internal const string MoqRepository = $"{Moq}.MockRepository";
    internal const string As = nameof(As);
    internal const string Create = nameof(Create);
    internal const string Of = nameof(Of);
}
