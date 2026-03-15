# Dependency Constraints and Version Management

## Critical Rule

Packages that ship inside the analyzer NuGet run in the **user's** compiler host, not our build environment. Versions must not exceed what the minimum supported SDK host provides (currently **.NET 8 SDK, assembly version 8.0.0.0**). This is the runtime host version in user IDEs, not the build SDK version (which is .NET 10).

## Enforcement

Build target `ValidateAnalyzerHostCompatibility` checks pinned package versions against .NET 8 SDK assembly versions. Builds fail if any pin exceeds host capabilities.

## Key Version Constraints (as of 2026-03-15)

| Package | Pinned Version | Reason |
|---------|---------------|--------|
| Microsoft.CodeAnalysis.CSharp | 4.8 | VS 2022 17.8+ support; newer = narrower VS compatibility |
| Microsoft.CodeAnalysis.CSharp.Workspaces | 4.8 | Matches above |
| Microsoft.CodeAnalysis.AnalyzerUtilities | 3.3.4 | **CAPPED** — 4.14.0+ refs System.Collections.Immutable 9.0.0 which fails to load in .NET 8 SDK hosts (CS8032). See issue #850. |

## Transitive Pins

`CentralPackageTransitivePinningEnabled=true` in Directory.Packages.props ensures transitive pins propagate throughout the dependency graph.

Transitive pins are declared in `Directory.Packages.props` under the `Label="Transitive pins"` item group. Adding new entries here must be reviewed against .NET 8 SDK assembly versions.

## Common Pitfall

If you see CS8032 ("An instance of analyzer ... cannot be created") in a user's build, the most likely cause is a transitive dependency version that exceeds the host SDK. Check if any recently added or upgraded package indirectly references a newer System.Collections.Immutable or other BCL assembly.

## References

- Issue #850 — documents the AnalyzerUtilities version cap
- `docs/dependency-management.md` — full explanation of the strategy
- `build/targets/codeanalysis/Packages.props` — code analysis package versions
- `build/targets/compiler/Packages.props` — compiler package versions
