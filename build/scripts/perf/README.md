# Benchmarking Instructions

When running benchmarks, use the `PerfCore.ps1` script located in `build/scripts/perf/` whenever possible. This script provides a more streamlined and consistent experience for running performance tests.

## Cross-Platform Support

The performance tools now support running on:
- **Windows**: Use `Perf.cmd` or run `PerfCore.ps1` directly with PowerShell
- **Linux/macOS**: Use `Perf.sh` (requires PowerShell Core to be installed)

ETL tracing is only available on Windows and requires admin permissions. On Linux/macOS, ETL will be automatically disabled with a warning message.

## Using PerfCore.ps1

The recommended way to run benchmarks is:

**Windows:**
```powershell
./build/scripts/perf/PerfCore.ps1 -projects "<relative-path-to-project>" [-filter "<test-filter>"] [-etl] [-ci] [-diff] [-v <verbosity>]
```
or
```cmd
Perf.cmd -projects "<relative-path-to-project>" [-filter "<test-filter>"] [-etl] [-ci] [-diff] [-v <verbosity>]
```

**Linux/macOS:**
```bash
./Perf.sh -projects "<relative-path-to-project>" [-filter "<test-filter>"] [-ci] [-diff] [-v <verbosity>]
```

### Parameters

- `-projects`: Required. Semi-colon delimited list of relative paths to benchmark projects.
- `-filter`: Optional. Filter for tests to run (supports wildcards).
- `-etl`: Optional. Capture ETL traces of performance tests (Windows only, requires admin permissions).
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

**Windows:**
```powershell
# Basic usage with PerfCore.ps1
./build/scripts/perf/PerfCore.ps1 -projects "tests/Moq.Analyzers.Performance.Tests" -filter "*Benchmark*" -v detailed

# Using Perf.cmd wrapper
Perf.cmd -projects "tests/Moq.Analyzers.Performance.Tests" -filter "*Benchmark*" -v detailed

# Using RunPerfTests.ps1 when PerfCore.ps1 is not available
./build/scripts/perf/RunPerfTests.ps1 -projects "tests/Moq.Analyzers.Performance.Tests" -filter "*Benchmark*" -perftestRootFolder ".." -output "artifacts/perf-results" -ci
```

**Linux/macOS:**
```bash
# Basic usage with Perf.sh wrapper
./Perf.sh -projects "tests/Moq.Analyzers.Performance.Tests" -filter "*Benchmark*" -v detailed

# Direct PowerShell usage (requires pwsh to be installed)
pwsh -ExecutionPolicy ByPass -NoProfile -command "& \"./build/scripts/perf/PerfCore.ps1\" -projects \"tests/Moq.Analyzers.Performance.Tests\" -filter \"*Benchmark*\" -v detailed"
```
