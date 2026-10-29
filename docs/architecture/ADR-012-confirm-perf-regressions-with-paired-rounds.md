---
title: "ADR-012: Confirm Performance Regressions with Paired Rounds"
status: "Accepted"
date: "2026-10-28"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "performance", "testing", "ci"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted. This ADR amends ADR-008. PerfDiff still decides, but it now decides in two stages.

## Context

The perf job ran the baseline build once and the current build once. The two runs were about 35 minutes apart on a shared runner. PerfDiff compared them and failed the job on any tripped gate.

Every iteration of one benchmark process shares one machine state. The iterations measure noise inside that state. They do not measure the drift between the two runs. A runner that slows down between runs looks the same as code that slows down.

Nightly run 40625434341 on 2026-10-28 showed the problem. No analyzer code changed since the baseline. Moq1200 at 1000 files had a baseline median of 224.4 ms and a current median of 244.1 ms. The current first quartile was above the baseline maximum. Any test on those iterations calls the shift real, and the 250 ms P95 budget failed. Earlier runs failed the same way on Moq1100 and Moq1002.

The job also tested about 88 benchmarks with five gates and no correction for testing many benchmarks at once.

## Decision

The first comparison becomes a screen. When a gate trips, PerfDiff writes the flagged benchmarks to `flagged.json`. The job then runs confirmation rounds for those benchmarks only.

1. Each round runs the baseline build and the current build as two separate processes. Odd rounds run the baseline first. Even rounds run it second. Linear drift over the job then cancels out.
2. Each round gives one log ratio, `ln(current / baseline)`, of the metric that tripped the gate.
3. The stop rule copies BenchmarkDotNet's rule for iterations. After at least 3 rounds, stop when the 99% confidence margin of the mean log ratio is below 2%. Stop at 10 rounds or after a 40 minute budget in any case. The rule reads only the spread of the ratios, never their direction, so it does not bias the test.
4. The decision is a one-sided paired t-test. The null hypothesis is that the slowdown is at most the gate's own minimum effect: 5% for the mean and P95 ratio gates, 35% for the median gate, and zero for the fixed budgets.
5. Holm's method corrects for every flagged benchmark at once. The family-wise error rate is 1%. A budget hypothesis also needs the median of the current rounds to stay over budget.
6. Only a confirmed regression fails the job. A screen result that does not confirm logs a warning.

The confirmation data is new and independent of the screen data. Choosing what to confirm from the screen does not bias the confirmation test.

## Consequences

### Positive

- **POS-001**: A failed perf job means a regression that paired rounds reproduced at a 1% family-wise error rate.
- **POS-002**: The job spends extra time only when the screen flags something.
- **POS-003**: Each flagged benchmark logs its ratio, confidence interval, threshold, and p-value.

### Negative

- **NEG-001**: A flagged run takes longer, up to the 40 minute budget. The job timeout grows to cover it.
- **NEG-002**: A regression smaller than its gate's minimum effect does not fail the job, even when it is real.
- **NEG-003**: With few rounds and high noise, a real regression can fail to confirm. The power depends on the runner's noise.

## Alternatives Considered

### Always run alternating full suites

- **ALT-001**: **Description**: Run the full suite in alternating baseline and current rounds on every run.
- **ALT-002**: **Rejection Reason**: About 3 to 4 hours per nightly run on hosted runners.

### Change only the PerfDiff math

- **ALT-003**: **Description**: Add significance tests and multiple-test correction to the single screen run.
- **ALT-004**: **Rejection Reason**: One run per side cannot separate runner drift from code. The 2026-10-28 failure would still fail.

## Implementation Notes

- **IMP-001**: `src/tools/PerfDiff/BDN/Confirmation/ConfirmationAnalyzer.cs` holds the stop rule, the t-test, and Holm's method.
- **IMP-002**: `PerfDiff confirm --flagged <file> --rounds <dir>` exits 0 for not confirmed, 3 for another round, 4 for confirmed, and 1 for an error.
- **IMP-003**: `Invoke-PerfConfirmation` in `build/scripts/perf/PerfBaselineManager.psm1` runs the rounds.

## References

- **REF-001**: ADR-008, BenchmarkDotNet and PerfDiff for Performance Regression Detection.
- **REF-002**: BenchmarkDotNet `EngineActualStage` stop rule, v0.15.8: 2% maximum relative error, 15 to 100 iterations.
