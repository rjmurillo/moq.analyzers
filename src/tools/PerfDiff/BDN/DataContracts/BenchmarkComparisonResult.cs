// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of comparing two benchmarks, including success and regression detection flags.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct BenchmarkComparisonResult(bool CompareSucceeded, bool RegressionDetected)
{
    /// <summary>
    /// Deconstructs the result into compareSucceeded and regressionDetected values.
    /// </summary>
    /// <param name="compareSucceeded">Indicates whether the comparison succeeded.</param>
    /// <param name="regressionDetected">Indicates whether a regression was detected.</param>
    public void Deconstruct(out bool compareSucceeded, out bool regressionDetected)
    {
        compareSucceeded = CompareSucceeded;
        regressionDetected = RegressionDetected;
    }
}
