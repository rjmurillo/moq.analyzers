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

ADR-003 pinned Roslyn to 4.8 so the analyzer loaded in VS 2022 17.8 and .NET 8 SDK hosts. ADR-004 capped AnalyzerUtilities at 3.3.4 for the same .NET 8 host. Both ADRs named a raised minimum host as the trigger to revisit them.

.NET 8 and .NET 9 reach end of support on November 10, 2026. The project now treats .NET 10 as the minimum supported version.

The .NET 10.0.100 SDK ships these assemblies in its compiler host:

| Assembly | Version |
| --- | --- |
| `Microsoft.CodeAnalysis` | 5.0.0.0 |
| `Microsoft.CodeAnalysis.CSharp` | 5.0.0.0 |
| `System.Collections.Immutable` | 10.0.0.0 |
| `System.Reflection.Metadata` | 10.0.0.0 |

The .NET 10 SDK needs MSBuild 18.0.0 (`sdk/10.0.100/minimumMSBuildVersion`). MSBuild 18 ships with VS 2026. VS 2022 (MSBuild 17) cannot use the .NET 10 SDK.

The `Microsoft.CodeAnalysis.CSharp` 5.0.0 package depends on `System.Collections.Immutable` and `System.Reflection.Metadata` 9.0.0. Every Roslyn 5.0 host provides at least 9.0.0.0. That includes the .NET Framework host in VS 2026 and `msbuild.exe`.

## Decision

- Pin `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.CSharp.Workspaces` to 5.0.0.
- Pin `Microsoft.CodeAnalysis.AnalyzerUtilities` to 5.0.0. Renovate caps it below 5.6.0. Version 5.6.0 needs `Microsoft.Bcl.AsyncInterfaces` 10.0.1.
- Pin `System.Collections.Immutable` to 9.0.0. Set the host ceiling for it and for `System.Reflection.Metadata` to major version 9.
- Test only .NET 10 hosts in the `analyzer-load-test` CI job: the dotnet CLI and VS 2026 `msbuild.exe`.

The analyzer and code fix assemblies still target `netstandard2.0` (ADR-002). The .NET Framework host in VS 2026 needs it.

## Consequences

### Positive

- **POS-001**: Roslyn 5.0 APIs and C# 14 are available to analyzers and tests.
- **POS-002**: The default test harness compiles at C# 14, the language that .NET 10 users write.
- **POS-003**: AnalyzerUtilities is no longer held on the 3.x line.

### Negative

- **NEG-001**: Users on VS 2022 or the .NET 8 and .NET 9 SDKs no longer get diagnostics. Their compiler is older than Roslyn 5.0, so it reports warning CS9057 and skips the analyzer.
- **NEG-002**: The shipped DLLs now reference `Microsoft.Bcl.AsyncInterfaces` 9.0.0.0, which the package does not bundle. A build with the .NET 10.0.100 SDK loaded and ran the analyzers with no CS8032 or AD0001. The VS 2026 host is covered by the `analyzer-load-test` CI job.

## Reversibility

This decision is **reversible** only by going back to the ADR-003 and ADR-004 pins. That would also bring back the .NET 8 host limits.

## References

- **REF-001**: ADR-003 -- Pin Roslyn SDK to Microsoft.CodeAnalysis 4.8 (superseded)
- **REF-002**: ADR-004 -- Cap Microsoft.CodeAnalysis.AnalyzerUtilities at 3.3.4 (superseded)
- **REF-003**: ADR-005 -- Central Package Management with Transitive Pinning
- **REF-004**: GitHub issue #850 -- CS8032 when a dependency exceeds the host version
- **REF-005**: .NET SDK, MSBuild, and Visual Studio versioning: <https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs>
