using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.CodeFixes;

internal static class CodeFixContextExtensions
{
    public static bool TryGetEditProperties(this CodeFixContext context, [NotNullWhen(true)] out DiagnosticEditProperties? editProperties)
    {
        ImmutableDictionary<string, string?> properties = context.Diagnostics[0].Properties;

        return DiagnosticEditProperties.TryGetFromImmutableDictionary(properties, out editProperties);
    }
}
