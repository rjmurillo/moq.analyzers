namespace Moq.Analyzers.Common;

#pragma warning disable ECS0200 // Consider using readonly instead of const for flexibility

internal static class DiagnosticCategory
{
    internal const string Moq = nameof(Moq);
    internal const string Usage = nameof(Usage);
}
