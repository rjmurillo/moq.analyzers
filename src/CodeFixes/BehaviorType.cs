﻿namespace Moq.CodeFixes;

/// <summary>
/// Options to customize the behavior of Moq.
/// </summary>
/// <remarks>
/// Duplicate of Moq's MockBehavior enum. There is no dependency on Moq
/// library in this project, so the values are duplicated.
/// </remarks>
internal enum BehaviorType
{
    /// <summary>
    /// Will never throw exceptions, returning default values when necessary
    /// (<see langword="null" /> for reference types, zero for value types,
    /// or empty for enumerables and arrays).
    /// </summary>
    Loose,

    /// <summary>
    /// Causes Moq to always throw an exception for invocations that don't have
    /// a corresponding Setup.
    /// </summary>
    Strict,
}
