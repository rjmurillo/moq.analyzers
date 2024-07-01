namespace Moq.Analyzers.Common;

internal static class DiagnosticIdExtensions
{
    internal static string ToHelpLinkUrl(this string id)
    {
        if (!id.StartsWith(WellKnownTypeNames.Moq, StringComparison.Ordinal))
        {
            throw new NotSupportedException();
        }

        // This is always "Moq" + 4 digits
        ReadOnlySpan<char> numericId = id.AsSpan().Slice(3, 4);

        return $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{numericId.ToString()}.md";
    }
}
