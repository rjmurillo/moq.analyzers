# CI Performance Benchmarking and Baseline Caching

## Overview

The CI workflow for this repository supports automated performance benchmarking with baseline result caching and manual override capabilities. This ensures efficient CI runs and gives maintainers control over when to refresh baseline results for performance validation.

## Baseline Caching

- Baseline performance results are cached for the SHA specified in `build/perf/baseline.json`.
- Results are stored per OS and SHA in `artifacts/performance/perfResults/baseline`.
- On subsequent runs, the workflow restores the cache and uses the cached results for performance comparisons, avoiding redundant benchmark runs.

## Forcing Baseline Benchmark Runs

- To force a fresh baseline run (even if cached results exist), use the `force_baseline` input in the workflow_dispatch UI or set the `FORCE_BASELINE` environment variable to `true`.
- This will ignore the cache and re-run the baseline benchmarks for the specified SHA.

## Manual Workflow Dispatch

- You can manually trigger the performance job via the GitHub Actions UI.
- Set `run_performance` to `true` to run the performance job.
- Set `force_baseline` to `true` to force a fresh baseline run.

## Script Behavior

- When running with the `-diff` flag, the performance scripts will:
  - Use cached baseline results if available (unless forced).
  - Skip checkout and baseline test runs if using cached results.
  - Always run benchmarks for the current commit.

## Example: Manual Run

1. Go to the Actions tab in GitHub.
2. Select the main workflow.
3. Click "Run workflow".
4. Set `run_performance` to `true`.
5. Set `force_baseline` to `true` if you want to re-run baseline benchmarks.

## Regression Detection Thresholds

### Benchmark Size and Statistical Significance

Performance benchmarks are run with 1 file by default in PR actions. This ensures that analyzer performance is measured quickly, to avoid any obvious regressions. Nightly, the full suite is run with baseline (no analyzers) and analyzers enabled on the one file and on 1,000 files. This provides a more realistic, statistically significant workload. The number of code files is controlled by each benchmark through a parameter, allowing BenchmarkDotNet to control the number of iterations required to provide stable and statistically significant results for comparison.

Performance regressions are detected using the following thresholds in the PerfDiff tool:

- **Mean (Average) absolute regression:** Fails if the mean execution time increases by more than 100 ms.
- **Mean based on performance ratio threshold:** requiring a 5% relative threshold and absolute threshold of 0.5ms to be exceeded over baseline.
- **95th Percentile Regression:** Fails if the 95th percentile execution time exceeds 250 ms.
- **Percentage Regression:** Fails if the median execution time increases by more than 35% compared to baseline.
- **Statistical Significance:** Uses the Mann-Whitney test to detect statistically significant regressions, with a user-supplied threshold.

These thresholds are hardcoded in the PerfDiff tool and are used during CI runs to automatically detect and fail on performance regressions. For more details on running benchmarks locally, see [build/scripts/perf/README.md](../build/scripts/perf/README.md).

## Future Direction: Practical Performance Budgets

As discussed in [issue #563](https://github.com/rjmurillo/moq.analyzers/issues/563), the project intends to move CI performance validation toward practical, user-facing performance budgets (e.g., "total analysis time < 500 ms for 1kLOC solution") and memory usage limits. This will ensure CI failures are actionable and relevant to real-world usage, reducing noise and improving developer feedback.

Currently, CI gating is based on microbenchmark regression thresholds (see above). Once benchmarks are updated to measure higher-level metrics, regression gates will be updated to fail only when these practical budgets are exceeded. The chosen budgets and rationale will be documented here when implemented.
