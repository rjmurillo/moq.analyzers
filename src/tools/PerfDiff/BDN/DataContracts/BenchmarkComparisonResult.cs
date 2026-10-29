// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using PerfDiff.BDN.Confirmation;

namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of comparing two benchmarks, including success and regression detection flags.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct BenchmarkComparisonResult(bool CompareSucceeded, bool RegressionDetected, IReadOnlyList<ConfirmationHypothesis>? Flagged = null)
{
    /// <summary>
    /// Gets the benchmarks that tripped a gate, or an empty list when none did.
    /// </summary>
    public IReadOnlyList<ConfirmationHypothesis> FlaggedOrEmpty => Flagged ?? [];
}
