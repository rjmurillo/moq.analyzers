---
title: "ADR-008: BenchmarkDotNet and PerfDiff for Performance Regression Detection"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "performance", "testing", "ci"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

Roslyn analyzers run on every keystroke in an IDE. A regression of a few milliseconds per analyzed file is user-visible as editor lag. Without measurement, performance regressions accumulate invisibly. Code reviewers cannot detect them by reading a diff.

BenchmarkDotNet is the standard .NET microbenchmarking library. It handles JIT warmup, timer resolution, statistical analysis, and result formatting. It produces machine-readable JSON output suitable for automated comparison.

The project requires a mechanism to compare benchmark results from a PR branch against a committed baseline and block merges when regressions exceed defined thresholds. No off-the-shelf tool in the ecosystem matches the project's specific JSON output format and threshold requirements, so a lightweight custom CLI tool was built.

## Decision

Analyzer performance is measured with BenchmarkDotNet in `tests/Moq.Analyzers.Benchmarks/`. Baseline results are stored in `build/perf/`. A custom CLI tool, PerfDiff (`src/tools/PerfDiff/`), compares new benchmark output against the baseline and exits with a non-zero code when regressions exceed thresholds. PerfDiff is invoked in CI and blocks PR merges on regression.

PerfDiff uses System.CommandLine 2.0.3 for CLI argument parsing.

## Consequences

### Positive

- **POS-001**: Performance regressions are detected automatically before they reach users. The gate is enforced in CI, not by reviewer discipline.
- **POS-002**: BenchmarkDotNet output is statistically rigorous. Results are not influenced by background noise from a single run.
- **POS-003**: Baseline data in `build/perf/` is versioned with the codebase. Reviewers can see the baseline alongside the code that sets it.

### Negative

- **NEG-001**: Contributors changing analyzer logic must run benchmarks locally before submitting a PR. Benchmark runs are slow (several minutes).
- **NEG-002**: Baseline data must be updated when intentional performance improvements are made. Stale baselines cause false regression failures.
- **NEG-003**: PerfDiff is a custom tool. It must be maintained alongside the project. Any changes to BenchmarkDotNet output format require PerfDiff updates.
- **NEG-004**: Benchmark results are sensitive to the machine running them. CI hardware variation can cause threshold breaches that are not real regressions.

## Alternatives Considered

### Manual Benchmarking

- **ALT-001**: **Description**: Require contributors to run benchmarks and include results in PR descriptions as a convention.
- **ALT-002**: **Rejection Reason**: Not enforced. History shows that unforced conventions are skipped under time pressure. Latency accumulates without measurement.

### No Performance Testing

- **ALT-003**: **Description**: Rely on user feedback to detect performance problems after release.
- **ALT-004**: **Rejection Reason**: User-reported performance issues arrive after the regression has shipped. Diagnosing the root commit is difficult. Users who notice lag may abandon the package silently.

### Third-Party Benchmark Comparison Service

- **ALT-005**: **Description**: Use a hosted service (e.g., Bencher.dev) to store and compare benchmark results.
- **ALT-006**: **Rejection Reason**: Introduces an external service dependency in the CI pipeline. Adds account management overhead. The project's requirements are simple enough for a self-contained CLI tool.

## Implementation Notes

- **IMP-001**: `tests/Moq.Analyzers.Benchmarks/` contains one benchmark class per analyzer. Benchmarks analyze representative C# source files.
- **IMP-002**: Baseline JSON files in `build/perf/` are updated by running `dotnet run --project src/tools/PerfDiff/ -- update` after a confirmed improvement.
- **IMP-003**: PerfDiff threshold configuration is in `build/perf/thresholds.json`. Thresholds are expressed as percentage regression limits per benchmark. Default threshold is 10% regression for mean execution time. Thresholds may be tightened for hot-path analyzers or loosened for cold-path initialization benchmarks. When CI fails due to threshold breach, contributors should (a) verify the regression is real by running locally, (b) optimize if real, or (c) update the baseline if the regression is acceptable.
- **IMP-004**: PerfDiff uses System.CommandLine 2.0.3. Any update to that dependency must go through the central package management review process per ADR-005.

## References

- **REF-001**: `src/tools/PerfDiff/` -- PerfDiff CLI tool source
- **REF-002**: `tests/Moq.Analyzers.Benchmarks/` -- benchmark project
- **REF-003**: `build/perf/` -- baseline data and thresholds
- **REF-004**: ADR-005 -- Central Package Management with Transitive Pinning
- **REF-005**: BenchmarkDotNet documentation: <https://benchmarkdotnet.org/>
