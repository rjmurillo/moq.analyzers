# Dependency Constraints and Version Management

## Critical Rule

Packages that ship inside the analyzer NuGet run in the **user's** compiler host, not our build environment. Versions must not exceed what the minimum supported SDK host provides (currently **.NET 10 SDK with Roslyn 5.0, which guarantees assembly version 9.0.0.0** for System.Collections.Immutable and System.Reflection.Metadata; see ADR-011). This is the runtime host version in user IDEs, not the build SDK version (which is .NET 10).

## Enforcement

Build target `ValidateAnalyzerHostCompatibility` checks that System.Collections.Immutable and System.Reflection.Metadata do not resolve above major version 9. Builds fail if any pin exceeds host capabilities.

## Key Version Constraints (as of 2026-10-25, ADR-011)

| Package | Pinned Version | Reason |
|---------|---------------|--------|
| Microsoft.CodeAnalysis.CSharp | 5.0.0 | Ships in the .NET 10.0.100 SDK. The .NET 10 SDK needs MSBuild 18 (VS 2026). |
| Microsoft.CodeAnalysis.CSharp.Workspaces | 5.0.0 | Matches above |
| Microsoft.CodeAnalysis.AnalyzerUtilities | 5.0.0 | Renovate caps it below 5.6.0. Version 5.6.0 pulls Microsoft.Bcl.AsyncInterfaces 10.0.1. See issue #850. |
| System.Collections.Immutable | 9.0.0 | Roslyn 5.0.0 depends on 9.0.0, so every Roslyn 5.0 host has it. |

Before ADR-011, the pins were Roslyn 4.8, AnalyzerUtilities 3.3.4, and SCI 8.0.0 for .NET 8 SDK hosts.

## Transitive Pins

`CentralPackageTransitivePinningEnabled=true` in Directory.Packages.props ensures transitive pins propagate throughout the dependency graph.

Transitive pins are declared in `Directory.Packages.props` under the `Label="Transitive pins"` item group. Adding new entries here must be reviewed against the minimum host assembly versions (ADR-011).

## Common Pitfall

If you see CS8032 ("An instance of analyzer ... cannot be created") in a user's build, the most likely cause is a transitive dependency version that exceeds the host SDK. Check if any recently added or upgraded package indirectly references a newer System.Collections.Immutable or other BCL assembly.

## References

- Issue #850 — documents the AnalyzerUtilities version cap
- `docs/dependency-management.md` — full explanation of the strategy
- `build/targets/codeanalysis/Packages.props` — code analysis package versions
- `build/targets/compiler/Packages.props` — compiler package versions
