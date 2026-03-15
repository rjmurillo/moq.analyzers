---
title: "ADR-003: Pin Roslyn SDK to Microsoft.CodeAnalysis 4.8"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "roslyn", "versioning"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

`Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces` are the core Roslyn packages that analyzers compile against. Roslyn uses a rolling version scheme tied to Visual Studio releases.

The version chosen as the compile-time reference establishes the minimum IDE version users must have. Referencing a newer version excludes users on older IDE releases. Referencing a version that is too old prevents use of APIs introduced in later Roslyn releases.

Roslyn 4.8 corresponds to Visual Studio 2022 17.8, released in November 2023. This release is broadly deployed and represents a reasonable minimum baseline. The APIs available in 4.8 are sufficient for all current analyzer implementations.

## Decision

`Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces` are pinned at version 4.8 in `Directory.Packages.props`. No analyzer or code fix project references a version outside this pin.

## Consequences

### Positive

- **POS-001**: Users on Visual Studio 2022 17.8 and later receive full analyzer functionality.
- **POS-002**: The API surface is stable and well-documented. No risk of depending on preview or unstable APIs.
- **POS-003**: Roslyn 4.8 provides `IOperation` support sufficient for all current analyzers.

### Negative

- **NEG-001**: Roslyn APIs introduced after 4.8 are unavailable. New analyzer features requiring newer APIs are blocked until the pin is raised.
- **NEG-002**: Users on Visual Studio 2022 releases earlier than 17.8 do not receive diagnostics. The package loads but reports no results.
- **NEG-003**: The pin must be reviewed when VS 2022 17.8 reaches end-of-support or when a required API exists only in a later Roslyn release.

### Reversibility

This decision is **reversible**. Raising the pin to a newer Roslyn version requires updating `Directory.Packages.props`, running the full test suite, verifying benchmarks, and updating documentation. The cost is low; the constraint is user impact assessment.

## Alternatives Considered

### Latest 4.x Roslyn Release

- **ALT-001**: **Description**: Pin to the most recent Roslyn 4.x release to access all current APIs.
- **ALT-002**: **Rejection Reason**: Each version bump raises the minimum IDE requirement. Frequent bumps fragment the user base and generate support issues from users on slightly older VS 2022 releases.

### Older 4.x Roslyn Release (e.g., 4.3)

- **ALT-003**: **Description**: Pin to an earlier 4.x release to maximize IDE compatibility.
- **ALT-004**: **Rejection Reason**: Missing IOperation APIs and other improvements needed by current analyzers. Would require workarounds that increase code complexity and reduce correctness.

## Implementation Notes

- **IMP-001**: The version pin is declared in `Directory.Packages.props`. All `.csproj` references to `Microsoft.CodeAnalysis.CSharp` omit version attributes per central package management policy.
- **IMP-002**: **Upgrade Trigger Criteria**: Evaluate a pin upgrade when (a) VS 2022 17.8 falls below 10% market share in Visual Studio telemetry, (b) a required Roslyn API exists only in a later version, or (c) 18 months have elapsed since the last evaluation. When upgrading, verify the new minimum VS version is acceptable, run all benchmark baselines, and update this ADR.
- **IMP-003**: The `ValidateAnalyzerHostCompatibility` MSBuild target checks Roslyn version compatibility as part of the build.

## References

- **REF-001**: ADR-005 -- Central Package Management with Transitive Pinning
- **REF-002**: Visual Studio 2022 release history: <https://learn.microsoft.com/en-us/visualstudio/releases/2022/release-history>
- **REF-003**: `Directory.Packages.props` -- authoritative version declarations
