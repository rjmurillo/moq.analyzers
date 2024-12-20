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

    public EditType TypeOfEdit { get; init; }

    public int EditPosition { get; init; }
}
