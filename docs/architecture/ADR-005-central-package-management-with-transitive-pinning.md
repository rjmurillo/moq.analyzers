---
title: "ADR-005: Central Package Management with Transitive Pinning"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "nuget", "dependencies"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

The analyzer DLL ships to users and executes inside their compiler host. Any transitive dependency that resolves to an assembly version the host does not provide causes a load failure. These failures are silent from the user's perspective: diagnostics do not fire and no build error points to the root cause.

Without central version management, each project declares its own package versions. Versions drift across projects. Transitive upgrades happen silently when any direct dependency pulls in a newer transitive version. There is no single place to audit what version constraints are in force.

NuGet's Central Package Management (CPM) feature consolidates all version declarations into `Directory.Packages.props`. Setting `CentralPackageTransitivePinningEnabled=true` extends this to transitive dependencies, preventing them from upgrading beyond what is declared centrally.

## Decision

All NuGet package versions are declared in `Directory.Packages.props`. `CentralPackageTransitivePinningEnabled` is set to `true`. Individual `.csproj` files reference packages by name only, without version attributes. A `ValidateAnalyzerHostCompatibility` MSBuild target audits pinned versions against .NET 8 SDK assembly versions at build time.

## Consequences

### Positive

- **POS-001**: All version decisions are visible and auditable in one file.
- **POS-002**: Transitive upgrades are blocked. No dependency silently upgrades to a version that breaks host compatibility.
- **POS-003**: The build-time validation target catches compatibility violations before they reach users.
- **POS-004**: Dependabot PRs touch `Directory.Packages.props` exclusively, making version change reviews straightforward.

### Negative

- **NEG-001**: Adding a new dependency requires editing `Directory.Packages.props` as a separate step from the `.csproj`. Contributors unfamiliar with CPM encounter this as friction.
- **NEG-002**: Transitive pinning can block legitimate upgrades. Each transitive version change requires a deliberate review.
- **NEG-003**: The `ValidateAnalyzerHostCompatibility` target must be kept current as the minimum supported SDK changes.

## Alternatives Considered

### Per-Project Package Versions

- **ALT-001**: **Description**: Each `.csproj` declares its own `<PackageReference Version="...">` attributes.
- **ALT-002**: **Rejection Reason**: No central enforcement. Version drift between projects is invisible. Transitive upgrades happen without review. Host compatibility failures reach users.

### Floating Versions

- **ALT-003**: **Description**: Use version ranges (e.g., `[4.*, )`) to allow automatic upgrades.
- **ALT-004**: **Rejection Reason**: Builds are non-deterministic. A transitive upgrade on any given build day can silently introduce a host incompatibility. This violates the correctness requirement for shipped analyzer packages.

## Implementation Notes

- **IMP-001**: `Directory.Packages.props` is located at the solution root. It sets `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>`.
- **IMP-002**: The `ValidateAnalyzerHostCompatibility` target runs during the `Build` target and compares pinned versions against a known-good .NET 8 SDK assembly manifest.
- **IMP-003**: When proposing a new dependency, open `Directory.Packages.props`, add the pin, verify host compatibility, and reference the ADR in the PR description.

## References

- **REF-001**: ADR-004 -- Cap Microsoft.CodeAnalysis.AnalyzerUtilities at 3.3.4
- **REF-002**: NuGet Central Package Management documentation: <https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management>
- **REF-003**: `Directory.Packages.props` -- authoritative version declarations
- **REF-004**: `docs/dependency-management.md` -- dependency version policy
