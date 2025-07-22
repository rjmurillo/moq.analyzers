// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace PerfDiff.Logging;

/// <summary>
/// Provides a no-op scope for logging operations.
/// </summary>
internal sealed class NullScope : IDisposable
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullScope"/>.
    /// </summary>
    public static NullScope Instance { get; } = new NullScope();

    /// <summary>
    /// Prevents external instantiation of the <see cref="NullScope"/> class.
    /// </summary>
    private NullScope()
    {
    }

    /// <summary>
    /// Disposes the scope. No operation performed.
    /// <summary>
    /// Performs no operation when disposing the scope.
    /// </summary>
    public void Dispose()
    {
    }
}
