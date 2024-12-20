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
}
