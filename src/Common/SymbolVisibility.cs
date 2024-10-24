namespace Moq.Analyzers.Common;

internal enum SymbolVisibility
{
    /// <summary>
    /// Public symbol visibility.
    /// </summary>
    Public = 0,

    /// <summary>
    /// Internal symbol visibility.
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Private symbol visibility.
    /// </summary>
    Private = 2,

    /// <summary>
    /// Internal symbol visibility.
    /// </summary>
    Friend = Internal,
}
