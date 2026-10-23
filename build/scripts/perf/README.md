# Benchmarking Instructions

> **CI Performance Benchmarking and Baseline Caching:**
> This repository supports automated performance benchmarking in CI, with baseline result caching and manual override capabilities. Baseline results are cached per OS and SHA, and can be force-refreshed via workflow inputs. For details on regression thresholds, manual runs, and force options, see the [Performance Testing Guidelines](../../../CONTRIBUTING.md#performance-testing-guidelines) in CONTRIBUTING.md.

When running benchmarks, use the `PerfCore.ps1` script located in `build/scripts/perf/` whenever possible. This script provides a more streamlined and consistent experience for running performance tests. To allow [BenchmarkDotNet to set power plans](https://mawosoft.github.io/BenchmarkDotNet/articles/configs/powerplans.html) the script will automatically elevate.

## Cross-Platform Support

The performance tools now support running on:

- **Windows**: Use `Perf.cmd` in the repo root or `build\scripts\perf\CIPerf.cmd` to run like the CI
- **Linux/macOS**: Use `Perf.sh` in the repo root or `build/scripts/perf/CIPerf.sh`

The batch and shell files call out to PowerShell, which can run on Windows, Linux, and macOS. [Installation Instructions](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.5).

> NOTE: When you run performance tests, it is highly recommended to use `sudo`. If you are on Windows, you can enable [Sudo for Windows](https://learn.microsoft.com/en-us/windows/advanced-settings/sudo).

## Using PerfCore.ps1

The recommended way to run benchmarks is to use PowerShell.

```powershell
pwsh -ExecutionPolicy Bypass -NoProfile -File ./build/scripts/perf/PerfCore.ps1 -projects "<relative-path-to-project>" [-filter "<test-filter>"] [-etl] [-ci] [-diff] [-v <verbosity>]
```

> NOTE: ETL tracing is only available on Windows and requires admin permissions. If you have not run elevated (from Admin terminal or with `sudo`) the test process will automatically be elevated. On Linux/macOS, ETL will be automatically disabled with a warning message.

Each benchmark is written out to a results folder:

- for the baseline: `artifacts/performance/perfResults/baseline/results`
- for current: `artifacts/performance/perfResults/perfTest/results`

The files are overwritten on subsequent runs for your current branch. Baseline is not overwritten unless the environment variable is set.

In each folder, there are four files generated for each benchmark with a prefix of the fully qualified type containing the benchmark (e.g., `Moq.Analyzers.Benchmarks.Moq1000SealedClassBenchmarks`) with the following suffixes:

1. `-report-github.md` containing a Markdown table shown on the console.
1. `-report.html` containing an HTML version of the Markdown report.
1. `-report.csv` containing the summarized data shown in BenchmarkDotNet output with all columns.
1. `-full-compressed.json` containing the raw measurement data.

The JSON data is what is used by the performance comparison tools to determine if a regression exists for an analyzer.

### Parameters

**Common Settings:**

- `-v` or `-verbosity`: Optional. Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], or diag[nostic].
- `-help`: Optional. Print help and exit
- `-ci`: Optional. Run in CI mode (fail fast and keep all partial artifacts).

**Actions:**

- `-diff`: Optional. Compare to baseline perf results.

**Advanced settings:**

- `-etl`: Optional. Capture ETL traces of performance tests (Windows only, requires admin permissions).
- `-filter`: Optional. Filter for tests to run (supports wildcards).
- `-projects`: Required. Semi-colon delimited list of relative paths to benchmark projects.

## Notes

1. When using ETL tracing (`-etl`), ensure you have admin permissions.
2. The CI mode (`-ci`) is useful for development as it fails fast and keeps partial artifacts.
3. For comparing performance results, use the `-diff` flag with `PerfCore.ps1`.
4. Both scripts will automatically handle restoring and building the projects before running tests.

## Warm-up Run

With `-diff`, `PerfCore.ps1` runs one warm-up benchmark process before the baseline and current runs. It runs `*Moq1000SealedClassBenchmarks*` from the current branch. Its results go to `artifacts/performance/perfResults/warmup` and are not compared.

Why it exists: on CI runners, the first benchmark process in a job runs faster than every later one. Without the warm-up, the baseline always got that faster slot. Every PR then looked about 1.3x slower on `Moq1000WithDiagnostics(FileCount: 1)`, even with identical code. See [#1382](https://github.com/rjmurillo/moq.analyzers/issues/1382) for the measurements.

A smaller bias remains: `Moq1002WithDiagnostics(FileCount: 1)` still reads about 1.4x slower in the current run with identical code. See [#1385](https://github.com/rjmurillo/moq.analyzers/issues/1385).

The warm-up adds about one minute. It fails the comparison if it exits with an error or writes no report. If you rename `Moq1000SealedClassBenchmarks`, update `$script:PerfWarmupFilter` in `PerfBaselineManager.psm1`.

> NOTE: Without `-diff` there is no warm-up. That run is the first process, so its absolute numbers can read about 25% fast on CI runners.

## Same Runtime for Both Runs

The benchmarks run in-process, so each run uses the runtime of its own target framework. The pinned baseline can target an older framework than the current branch. For example, the baseline in `build/perf/baseline.json` targets `net8.0` while the current branch targets `net10.0`.

With `-diff`, `PerfCore.ps1` sets `DOTNET_ROLL_FORWARD=LatestMajor` for the warm-up, baseline, current, and compare steps. Every run then uses the newest installed runtime, so a runtime change cannot pass for an analyzer change. The previous value is restored afterward.

## Example Usage

You can run a quick pass of the benchmarks (about 20 minutes with baseline, then the baseline is reused)

```powershell
pwsh -ExecutionPolicy Bypass -NoProfile -File ./build/scripts/perf/PerfCore.ps1 -v diag -diff -ci -filter '*(FileCount: 1)'
```

This is similar to what is run in the [main action](../../../.github/workflows/main.yml) when you raise a PR. To run the full suite, use `-filter '*'` or omit the parameter.

> NOTE: If you _always_ want to run the baseline, you can set the environment variable `FORCE_PERF_BASELINE=true`

A cached baseline is reused only when it holds a benchmark report. A new baseline run first deletes the old baseline results. If a baseline run with a narrower filter writes no report, the script retries once with filter `*`. If there is still no report, the run stops with an error that names the baseline folder.

## Confirmation Rerun

Runner noise does not repeat. A real regression does. Round 1 runs the whole baseline, then the whole current branch, on one shared runner, so slow drift lands on one side. Before the nightly goes red, `PerfCore.ps1 -diff` therefore repeats only the flagged benchmarks.

1. The round 1 comparison runs with `--regressedOutput`. PerfDiff writes the ids of the benchmarks behind each tripped gate to `artifacts/performance/perfResults/regressed-ids.json`. The file is a JSON array of strings, sorted ordinally, without duplicates. It is `[]` when no gate tripped.
1. If round 1 exits with the regression code (3) and the file lists at least one id, the script reruns only those benchmarks, in the reverse order of round 1: current first into `perfResults/confirm/perfTest`, then baseline into `perfResults/confirm/baseline`. The baseline worktree from round 1 (`artifacts/perfBaseline`) is reused. If round 1 used a cached baseline, the script creates the worktree first.
1. The rerun comparison runs with `--confirmOnly` pointing at the round 1 file. A gate trips only on benchmarks that were flagged in round 1 too. Only then does the job fail.

The log ends with `Round 1 flagged N, confirmed M.` M counts the benchmarks flagged again in round 2 that were also flagged in round 1.

The rerun filter holds one exact glob per flagged id. BenchmarkDotNet matches a filter against the full benchmark name, parameters included, for example `Moq.Analyzers.Benchmarks.Moq1200Benchmarks.Moq1200WithDiagnostics(FileCount: 1)`. Only `*` and `?` are wildcards and neither can be escaped, so `ConvertTo-PerfBenchmarkFilter` replaces them (and double quotes) with `?`, which matches that one character. Several globs go after a single `--filter` flag and are ORed.

No rerun happens when:

- round 1 passes (exit 0), or
- round 1 fails for any other reason (exit 1 comparison error, 2 cancelled, a build or run failure). A rerun would fail the same way, so the job fails as before, or
- the regression code comes with an empty or missing ids file. The job fails with that explanation.

The rerun also uses the warm-up and `DOTNET_ROLL_FORWARD=LatestMajor` setup from round 1. It runs `dotnet restore` and `dotnet build --no-incremental` once per side, which is acceptable for a short filtered run.

### PerfDiff exit codes

| Code | Meaning |
| ---- | ------- |
| 0 | No regression, regression without `--failOnRegression`, or a rerun that did not confirm |
| 1 | The comparison could not run (missing results, mismatched benchmark sets, unreadable or malformed `--confirmOnly` file, unwritable `--regressedOutput` path) |
| 2 | Cancelled |
| 3 | A regression gate tripped and `--failOnRegression` was set |

`PerfBaselineManager.psm1` holds the same value as `$script:PerfRegressionExitCode`. A Pester test compares it with `Program.RegressionExitCode` in `src/tools/PerfDiff/Program.cs`.

A missing or malformed `--confirmOnly` file is an error, never a silent pass. An empty array (`[]`) is valid and confirms nothing.

## Hysteresis on Absolute Gates

The absolute gates (P95 over 250 ms, mean over 100 ms) used to fail on any crossing of the line. A nightly failed with the baseline under 250 ms and the current run at 250.055 ms. The gates now use a 5% margin (`RegressionStrategyHelper.AbsoluteGateHysteresisMultiplier`):

- Worse: the run under test is above `threshold * 1.05` and the baseline is at or under `threshold`. For P95 that is above 262.5 ms; for mean it is above 105 ms.
- Better: the run under test is at or under `threshold` and the baseline is above `threshold * 1.05`.

A benchmark sitting on the line cannot flip the gate on a sub-5% wobble. The log lines state the effective limit. The ratio gates (5% geomean, 35% median) are unchanged.
