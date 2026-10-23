// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of comparing two benchmarks, including success and regression detection flags.
/// </summary>
/// <param name="CompareSucceeded">Whether the result sets could be loaded and compared.</param>
/// <param name="RegressionDetected">Whether at least one regression gate tripped.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct BenchmarkComparisonResult(bool CompareSucceeded, bool RegressionDetected)
{
    /// <summary>
    /// Gets the ids of the benchmarks that put a tripped gate over its limit, sorted ordinally and without duplicates.
    /// Empty when no gate tripped.
    /// </summary>
    public IReadOnlyList<string> RegressedIds { get; init; } = [];
}
