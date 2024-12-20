using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.CodeFixes;

internal static class CodeFixContextExtensions
{
    public static bool TryGetEditProperties(this CodeFixContext context, [NotNullWhen(true)] out DiagnosticEditProperties? editProperties)
    {
        ImmutableDictionary<string, string?> properties = context.Diagnostics[0].Properties;

        // Try parsing; if anything fails return false
        editProperties = null;
        if (!properties.TryGetValue(DiagnosticEditProperties.EditTypeKey, out string? editTypeString))
        {
            return false;
        }

        if (!properties.TryGetValue(DiagnosticEditProperties.EditPositionKey, out string? editPositionString))
        {
            return false;
        }

        if (!Enum.TryParse(editTypeString, out DiagnosticEditProperties.EditType editType))
        {
            return false;
        }

        if (!int.TryParse(editPositionString, out int editPosition))
        {
            return false;
        }

        editProperties = new DiagnosticEditProperties
        {
            TypeOfEdit = editType,
            EditPosition = editPosition,
        };

        return true;
    }
}
