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

## Notes

- This mechanism ensures efficient CI runs and gives maintainers control over when to refresh baseline results for performance validation.
- For more details on running benchmarks locally, see [build/scripts/perf/README.md](../build/scripts/perf/README.md).
