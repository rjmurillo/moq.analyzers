# PR and Issue Patterns

## Issue Distribution (200 total: 44 open, 156 closed)

- Bugs: 34 (17%)
- Enhancements: 31 (15.5%)
- Tech debt: 21 (10.5%)
- Performance: 8 (4%)
- Documentation: 11 (5.5%)

## PR Distribution (200 total: 181 merged, 18 closed unmerged, 1 open)

- Dependency updates (chore): 113 (56.5%)
- Bug fixes: 35 (17.5%)
- Refactors: 8 (4%)
- Tests: 8 (4%)
- Features: 6 (3%)
- Performance: 5 (2.5%)

## Key Pattern: Dependency Update PRs Dominate

56.5% of all PRs are dependency updates via Renovate/Dependabot.
18 dependency PRs were closed without merging due to analyzer host compatibility.
This led to Renovate version cap configuration (#1072).

## Key Pattern: v0.4.0 Was a High-Risk Release

Largest release with 200+ PRs merged. Caused:

- CS8032 assembly mismatch regression
- Multiple false positive regressions (#849, #887, #896)
- Lesson: Large batched releases amplify risk. Smaller, more frequent releases are safer.

## Key Pattern: Systematic Audits Create Bulk Issues

July 2025: 81 issues from one systematic audit.
Most carry "triage" label, indicating backlog needs grooming.

## Contributor Patterns

- Primary contributors: @rjmurillo, @MattKotsenas
- External contributor: @Youssef1313 (v0.3.1 bug fix)
- Community issues: sparse but high quality when they arrive

## Labels That Signal Priority

- `bug` + `analyzers`: User-facing false positive/negative. Fix urgently.
- `performance`: Analyzer hot-path issue. Affects IDE responsiveness.
- `correctness`: Accuracy-critical. Zero tolerance for error.
- `tech_debt` + `triage`: Backlog items needing prioritization.
- `epic`: Multi-issue effort requiring planning.
