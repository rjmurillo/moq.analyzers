namespace Moq.Analyzers.Common;

#pragma warning disable ECS0200 // Consider using readonly instead of const for flexibility

internal static class DiagnosticCategory
{
    internal const string Usage = nameof(Usage);

    internal const string Correctness = nameof(Correctness);

    internal const string BestPractice = "Best Practice";
}
