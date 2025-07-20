// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace PerfDiff.BDN.DataContracts;

[StructLayout(LayoutKind.Auto)]
public readonly record struct BenchmarkComparisonResult(bool CompareSucceeded, bool RegressionDetected)
{
    public void Deconstruct(out bool compareSucceeded, out bool regressionDetected)
    {
        compareSucceeded = CompareSucceeded;
        regressionDetected = RegressionDetected;
    }
}
