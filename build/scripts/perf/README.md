# Benchmarking Instructions

When running benchmarks, use the `PerfCore.ps1` script located in `build/scripts/perf/` whenever possible. This script provides a more streamlined and consistent experience for running performance tests.

## Using PerfCore.ps1

The recommended way to run benchmarks is:

```powershell
./build/scripts/perf/PerfCore.ps1 -projects "<relative-path-to-project>" [-filter "<test-filter>"] [-etl] [-ci] [-diff] [-v <verbosity>]
```

### Parameters

- `-projects`: Required. Semi-colon delimited list of relative paths to benchmark projects.
- `-filter`: Optional. Filter for tests to run (supports wildcards).
- `-etl`: Optional. Capture ETL traces of performance tests (requires admin permissions and Windows).
- `-ci`: Optional. Run in CI mode (fail fast and keep all partial artifacts).
- `-diff`: Optional. Compare to baseline perf results.
- `-v` or `-verbosity`: Optional. Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], or diag[nostic].

## Alternative Approach

If `PerfCore.ps1` cannot be used, you can use `RunPerfTests.ps1` with the following parameters:

```powershell
./build/scripts/perf/RunPerfTests.ps1 -projects "<relative-path-to-project>" -filter "<test-filter>" -perftestRootFolder "<root-folder>" -output "<output-folder>" [-etl] [-ci]
```

### Parameters

- `-projects`: Semicolon separated list of relative paths to benchmark projects to run.
- `-filter`: Filter for tests to run (supports wildcards).
- `-perftestRootFolder`: Root folder all benchmark projects share.
- `-output`: Folder to write benchmark results to.
- `-etl`: Optional. Capture ETL traces for performance tests.
- `-ci`: Optional. Run in CI mode.

## Notes

1. When using ETL tracing (`-etl`), ensure you have admin permissions.
2. The CI mode (`-ci`) is useful for development as it fails fast and keeps partial artifacts.
3. For comparing performance results, use the `-diff` flag with `PerfCore.ps1`.
4. Both scripts will automatically handle restoring and building the projects before running tests.

## Example Usage

```powershell
# Basic usage with PerfCore.ps1
./build/scripts/perf/PerfCore.ps1 -projects "tests/Moq.Analyzers.Performance.Tests" -filter "*Benchmark*" -v detailed

# Using RunPerfTests.ps1 when PerfCore.ps1 is not available
./build/scripts/perf/RunPerfTests.ps1 -projects "tests/Moq.Analyzers.Performance.Tests" -filter "*Benchmark*" -perftestRootFolder ".." -output "artifacts/perf-results" -ci
```
