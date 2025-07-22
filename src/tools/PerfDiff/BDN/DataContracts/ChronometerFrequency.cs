// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace PerfDiff.BDN.DataContracts;

/// <summary>
/// Represents the frequency of a hardware chronometer in Hertz.
/// </summary>
public class ChronometerFrequency
{
    /// <summary>
    /// Gets or sets the frequency in Hertz.
    /// </summary>
    public int Hertz { get; set; }
}
