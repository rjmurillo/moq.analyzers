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
./build/scripts/perf/PerfCore.ps1 -projects "<relative-path-to-project>" [-filter "<test-filter>"] [-etl] [-ci] [-diff] [-v <verbosity>]
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

## Example Usage

You can run a quick pass of the benchmarks (about 20 minutes with baseline, then the baseline is reused)

```powershell
./build/scripts/perf/PerfCore.ps1 -v diag -diff -ci -filter '*(FileCount: 1)'
```

This is similar to what is run in the [main action](../../../.github/workflows/main.yml) when you raise a PR. To run the full suite, use `-filter '*'` or omit the parameter.

> NOTE: If you _always_ want to run the baseline, you can set the environment variable `FORCE_PERF_BASELINE=true`
