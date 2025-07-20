// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the result of comparing two benchmarks, including success and regression detection flags.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct BenchmarkComparisonResult(bool CompareSucceeded, bool RegressionDetected)
{    
}
