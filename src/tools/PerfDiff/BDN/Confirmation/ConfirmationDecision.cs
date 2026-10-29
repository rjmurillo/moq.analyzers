namespace PerfDiff.BDN.Confirmation;

/// <summary>
/// The outcome of analyzing the confirmation rounds that ran so far.
/// </summary>
public enum ConfirmationDecision
{
    /// <summary>No flagged benchmark is a confirmed regression.</summary>
    NotConfirmed,

    /// <summary>The rounds cannot be read or paired.</summary>
    Error,

    /// <summary>The stop rule needs another round.</summary>
    Continue,

    /// <summary>At least one flagged benchmark is a confirmed regression.</summary>
    Confirmed,
}
