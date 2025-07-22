// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace PerfDiff.ETL;

internal sealed record OverWeightResult(string Name, float Before, float After, float Delta, float Overweight, float Percent, int Interest)
{
    /// <summary>
        /// Returns a formatted string summarizing the OverWeightResult, including the name, overweight percentage, before and after values in milliseconds, and interest.
        /// </summary>
        /// <returns>A string representation of the OverWeightResult.</returns>
        public override string ToString()
        => $"'{Name}':, Overweight: '{Overweight}%', Before: '{Before}ms', After: '{After}ms', Interest :'{Interest}'";
}
