namespace Moq.Analyzers.Common;

internal static class DiagnosticIdExtensions
{
    internal static string ToId(this DiagnosticId id) => FormattableString.Invariant($"Moq{(int)id:D4}");

    internal static string ToHelpLinkUrl(this DiagnosticId id)
    {
        return $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{id.ToId()}.md";
    }
}
