using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Moq.Analyzers.Common;

internal record class DiagnosticEditProperties
{
    internal static readonly string EditTypeKey = nameof(EditTypeKey);
    internal static readonly string EditPositionKey = nameof(EditPositionKey);

    /// <summary>
    /// The type of edit for the code fix to perform.
    /// </summary>
    internal enum EditType
    {
        /// <summary>
        /// Insert a new parameter, moving the existing parameters to position N+1.
        /// </summary>
        Insert,

        /// <summary>
        /// Replace the parameter without changing the overall number of parameters.
        /// </summary>
        Replace,
    }

    /// <summary>
    /// Gets the type of edit operation to perform.
    /// </summary>
    public EditType TypeOfEdit { get; init; }

    /// <summary>
    /// Gets the zero-based position where the edit should be applied.
    /// </summary>
    public int EditPosition { get; init; }

    /// <summary>
    /// Returns the current object as an <see cref="ImmutableDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <returns>The current object as an immutable dictionary.</returns>
    public ImmutableDictionary<string, string?> ToImmutableDictionary()
    {
        ImmutableDictionary<string, string?>.Builder builder = ImmutableDictionary.CreateBuilder<string, string?>(StringComparer.Ordinal);
        builder.Add(EditTypeKey, TypeOfEdit.ToString());
        builder.Add(EditPositionKey, EditPosition.ToString(CultureInfo.InvariantCulture));
        return builder.ToImmutable();
    }

    /// <summary>
    /// Tries to convert an immutable dictionary to a <see cref="DiagnosticEditProperties"/>.
    /// </summary>
    /// <param name="dictionary">The dictionary to try to convert.</param>
    /// <param name="editProperties">The output edit properties if parsing succeeded, otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> otherwise.</returns>
    public static bool TryGetFromImmutableDictionary(ImmutableDictionary<string, string?> dictionary, [NotNullWhen(true)] out DiagnosticEditProperties? editProperties)
    {
        editProperties = null;
        if (!dictionary.TryGetValue(EditTypeKey, out string? editTypeString))
        {
            return false;
        }

        if (!dictionary.TryGetValue(EditPositionKey, out string? editPositionString))
        {
            return false;
        }

        if (!Enum.TryParse(editTypeString, out EditType editType))
        {
            return false;
        }

        if (!int.TryParse(editPositionString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int editPosition))
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
