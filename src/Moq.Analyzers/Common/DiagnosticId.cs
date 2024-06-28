namespace Moq.Analyzers.Common;

/// <summary>
/// The diagnostic IDs for the analyzers.
/// </summary>
public enum DiagnosticId
{
    /// <summary>
    /// An explicit value is not set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The Mock arguments must match the constructor parameters.
    /// </summary>
    NoMatchingConstructor = 1002,
}
