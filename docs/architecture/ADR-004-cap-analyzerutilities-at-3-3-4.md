---
title: "ADR-004: Cap Microsoft.CodeAnalysis.AnalyzerUtilities at 3.3.4"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "dependencies", "compatibility"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

`Microsoft.CodeAnalysis.AnalyzerUtilities` provides helper types used across multiple analyzers. Starting with version 4.14.0, this package declares a dependency on `System.Collections.Immutable 9.0.0.0`.

When the analyzer runs inside a .NET 8 SDK host (`dotnet build` on .NET 8 SDK), that assembly version is not present. The host cannot resolve the binding, and the analyzer fails to instantiate. The user sees CS8032: "An instance of analyzer cannot be created from...". No diagnostics fire.

Version 3.3.4 declares a dependency on `System.Collections.Immutable 1.2.3.0`, which is available in all .NET 8 SDK host environments. This issue is tracked in GitHub issue #850.

The minimum host SDK will be raised to .NET 9 at a future date. Until then, the 4.x series of AnalyzerUtilities is incompatible with .NET 8 SDK users.

## Decision

`Microsoft.CodeAnalysis.AnalyzerUtilities` is explicitly capped at version 3.3.4 in `Directory.Packages.props`. A `ValidateAnalyzerHostCompatibility` MSBuild target enforces this cap at build time. Any attempt to upgrade to 4.x fails the build with a descriptive error.

## Consequences

### Positive

- **POS-001**: Analyzers load and execute correctly in .NET 8 SDK host environments.
- **POS-002**: The build-time enforcement prevents accidental upgrades via Dependabot or manual edits without a deliberate decision to raise the minimum SDK.
- **POS-003**: The constraint is documented and enforceable, eliminating silent runtime failures for users.

### Negative

- **NEG-001**: AnalyzerUtilities features introduced in 4.x are unavailable. New helpers must be implemented locally or worked around.
- **NEG-002**: The cap creates friction when Dependabot proposes version upgrades. Each proposal requires a manual rejection with a comment referencing this ADR.
- **NEG-003**: When the minimum host SDK is raised to .NET 9, this cap must be revisited and the `ValidateAnalyzerHostCompatibility` target updated.

## Alternatives Considered

### Use AnalyzerUtilities 4.x

- **ALT-001**: **Description**: Accept the 4.x upgrade and its `System.Collections.Immutable 9.0.0.0` dependency.
- **ALT-002**: **Rejection Reason**: Causes CS8032 failures for all users building with the .NET 8 SDK. This is a large portion of the user base and an unacceptable regression.

### Remove AnalyzerUtilities Dependency

- **ALT-003**: **Description**: Eliminate the dependency on `AnalyzerUtilities` entirely and inline any needed helpers.
- **ALT-004**: **Rejection Reason**: The helpers provided reduce boilerplate across multiple analyzers. Removing the dependency requires duplicating or reimplementing non-trivial utility code. The cost is not justified when capping the version solves the problem.

## Implementation Notes

- **IMP-001**: The version cap is declared in `Directory.Packages.props` with an explicit `<PackageVersion Include="Microsoft.CodeAnalysis.AnalyzerUtilities" Version="3.3.4" />`.
- **IMP-002**: The `ValidateAnalyzerHostCompatibility` MSBuild target is defined in `build/` and imported by all analyzer projects.
- **IMP-003**: When raising the minimum host SDK to .NET 9, remove the cap, verify CI passes on all supported host environments, and update this ADR to Superseded.

## References

- **REF-001**: GitHub issue #850 -- CS8032 with AnalyzerUtilities 4.x on .NET 8 SDK
- **REF-002**: ADR-005 -- Central Package Management with Transitive Pinning
- **REF-003**: `Directory.Packages.props` -- version cap declaration
- **REF-004**: `docs/dependency-management.md` -- dependency version policy
