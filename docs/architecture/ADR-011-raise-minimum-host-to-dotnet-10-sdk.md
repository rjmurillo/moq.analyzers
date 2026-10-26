---
title: "ADR-011: Raise the Minimum Supported Host to the .NET 10 SDK (Roslyn 5.0)"
status: "Accepted"
date: "2026-10-25"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "roslyn", "versioning", "compatibility"]
supersedes: "ADR-003, ADR-004"
superseded_by: ""
---

## Status

Accepted

## Context

ADR-003 pinned Roslyn to 4.8 so the analyzer loaded in VS 2022 17.8 and .NET 8 SDK hosts. ADR-004 capped AnalyzerUtilities at 3.3.4 for the same .NET 8 host. ADR-004 named a raised minimum host as its trigger to revisit. ADR-003 named other triggers in IMP-002: VS 2022 17.8 market share, a needed API, or 18 months. None of the ADR-003 triggers fired. This ADR overrides them by policy.

The policy is to drop .NET 8 and .NET 9. Both reach end of support on November 10, 2026 (REF-006). The project now treats .NET 10 as the minimum supported version.

VS 2022 itself stays in Microsoft support after that date. Dropping it is a deliberate trade-off. The project gives up VS 2022 users to move to Roslyn 5.0 and drop the .NET 8 host limits.

The .NET 10.0.100 SDK ships these assemblies in its compiler host:

| Assembly | Version |
| --- | --- |
| `Microsoft.CodeAnalysis` | 5.0.0.0 |
| `Microsoft.CodeAnalysis.CSharp` | 5.0.0.0 |
| `System.Collections.Immutable` | 10.0.0.0 |
| `System.Reflection.Metadata` | 10.0.0.0 |

The .NET 10 SDK needs MSBuild 18.0.0 (`sdk/10.0.100/minimumMSBuildVersion`). MSBuild 18 ships with VS 2026. VS 2022 (MSBuild 17) cannot use the .NET 10 SDK.

The `Microsoft.CodeAnalysis.CSharp` 5.0.0 package depends on `System.Collections.Immutable` and `System.Reflection.Metadata` 9.0.0. From that, we infer that every Roslyn 5.0 host provides at least 9.0.0.0. That includes the .NET Framework host in VS 2026 and `msbuild.exe`. This is inferred from the package, not measured on VS 2026. The `analyzer-load-test` VS 2026 legs are the measured check.

## Decision

- Pin `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces` to 5.0.0.
- Pin `Microsoft.CodeAnalysis.AnalyzerUtilities` to 5.0.0. Renovate caps it below 5.6.0. Version 5.6.0 needs `Microsoft.Bcl.AsyncInterfaces` 10.0.1.
- Pin `System.Collections.Immutable` to 9.0.0. Set the host ceiling for it and for `System.Reflection.Metadata` to major version 9.
- Test only .NET 10 hosts in the `analyzer-load-test` CI job: the dotnet CLI on SDK 10.0.100, and VS 2026 `msbuild.exe`.

Each pin is held at the floor of the new host, not at the latest version. Each one can move on its own later.

The analyzer and code fix assemblies still target `netstandard2.0` (ADR-002). The .NET Framework host in VS 2026 needs it.

## Consequences

### Positive

- **POS-001**: Roslyn 5.0 APIs and C# 14 are available to analyzers and tests.
- **POS-002**: The default test harness compiles at C# 14, the language that .NET 10 users write.
- **POS-003**: AnalyzerUtilities is no longer held on the 3.x line.

### Negative

- **NEG-001**: Users on VS 2022 or the .NET 8 and .NET 9 SDKs no longer get diagnostics. A build with the .NET 9.0.318 SDK (Roslyn 4.14.0.0) reported warning CS9057, exited 0, and reported no Moq diagnostics. Moq.Analyzers 0.4.2 is the last release for those hosts. README and `docs/install.md` say so.
- **NEG-002**: The shipped DLLs now reference `Microsoft.Bcl.AsyncInterfaces` 9.0.0.0, which the package does not bundle. A build with the .NET 10.0.100 SDK loaded and ran the analyzers with no CS8032 or AD0001. The `analyzer-load-test` job checks the VS 2026 host. Each leg must report Moq1000, so the analyzers must run, not only load.
- **NEG-003**: ADR-003 IMP-002 asks for benchmark baselines on an upgrade. Four benchmarks (Moq1201, Moq1203, Moq1206, Moq1208) changed their `MoqKey` names. They no longer match the pinned baseline in `build/perf/baseline.json`. The baseline must move to a `main` commit after this change.
- **NEG-004**: The change is one-way for each release. A later revert does not help users who already upgraded. The release notes must call out the new minimum host.

## Alternatives Considered

### Keep Roslyn 4.8 and move only the test projects

- **ALT-001**: **Description**: Keep the shipped pins from ADR-003 and ADR-004. Move only test and tool projects to `net10.0`.
- **ALT-002**: **Rejection Reason**: The project chose to deprecate .NET 8 and .NET 9 as hosts. This option keeps the .NET 8 host limits, AnalyzerUtilities 3.x, and a C# 12 test harness.

### Pin Roslyn 4.14

- **ALT-003**: **Description**: Pin Roslyn 4.14, which VS 2022 17.14 and the .NET 9.0.3xx SDK provide.
- **ALT-004**: **Rejection Reason**: It keeps a .NET 9 SDK band as the floor, and .NET 9 is being deprecated. A second host bump would follow soon.

### Ship one analyzer build per Roslyn version

- **ALT-005**: **Description**: Ship `analyzers/dotnet/roslyn4.8/cs` and `analyzers/dotnet/roslyn5.0/cs` in one package. The host loads the newest build that it supports.
- **ALT-006**: **Rejection Reason**: It doubles the shipped builds, the host checks, and the load-test matrix. The project chose to drop older hosts instead. See the review trigger below.

## Implementation Notes

- **IMP-001**: `Directory.Packages.props` holds the pins.
- **IMP-002**: `ValidateAnalyzerHostCompatibility` in `build/targets/packaging/Packaging.targets` blocks SCI, SRM, and `Microsoft.Bcl.AsyncInterfaces` above major 9.
- **IMP-003**: The `main.yml` build job and `AnalyzerAssemblyCompatibilityTests` check the shipped DLL references. The limits are SCI, SRM, and `Microsoft.Bcl.AsyncInterfaces` 9.0.0.0, `System.Threading.Channels` 8.0.0.0, and the Roslyn assemblies 5.0.0.0.
- **IMP-004**: The `analyzer-load-test` job fails on CS8032, CS9057, AD0001, an assembly binding error, or a missing Moq1000.

## Reversibility

This decision is **reversible** only by going back to the ADR-003 and ADR-004 pins. That would also bring back the .NET 8 host limits.

Review this decision if users on VS 2022 report lost diagnostics, or before the next host floor change. At that review, weigh ALT-005 again.

## References

- **REF-001**: ADR-003 -- Pin Roslyn SDK to Microsoft.CodeAnalysis 4.8 (superseded)
- **REF-002**: ADR-004 -- Cap Microsoft.CodeAnalysis.AnalyzerUtilities at 3.3.4 (superseded)
- **REF-003**: ADR-005 -- Central Package Management with Transitive Pinning
- **REF-004**: GitHub issue #850 -- CS8032 when a dependency exceeds the host version
- **REF-005**: .NET SDK, MSBuild, and Visual Studio versioning: <https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs>
- **REF-006**: .NET support policy: <https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core>
