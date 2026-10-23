// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace PerfDiff;

/// <summary>
/// The parsed options of one PerfDiff run, passed as a unit so the comparison entry points stay narrow.
/// </summary>
/// <param name="BaselineFolder">Folder with the baseline results.</param>
/// <param name="ResultsFolder">Folder with the results under test.</param>
/// <param name="FailOnRegression">When <see langword="true"/>, a regression returns <see cref="Program.RegressionExitCode"/>.</param>
/// <param name="RegressedOutputPath">
/// When set, the ids of the benchmarks that tripped a gate are written there as a JSON array.
/// The file is written on every successful comparison, as <c>[]</c> when no gate tripped.
/// </param>
/// <param name="ConfirmOnlyPath">
/// When set, only benchmarks listed in this file (written by <paramref name="RegressedOutputPath"/> in a
/// first round) are gated. A missing or malformed file is an error, never a silent pass.
/// </param>
internal sealed record PerfDiffRequest(
    string BaselineFolder,
    string ResultsFolder,
    bool FailOnRegression,
    string? RegressedOutputPath = null,
    string? ConfirmOnlyPath = null);
