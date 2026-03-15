# Release History and Evolution

## NuGet Package

- Package ID: `Moq.Analyzers`
- Authors: Matt Kotsenas, Andrey "Litee" Lipatkin, Richard Murillo
- License: BSD-3-Clause
- Target: netstandard2.0 (development dependency only)
- Versioning: Nerdbank.GitVersioning (NBGV), SemVer 2
- Current version stem: 0.5.0-alpha.{height}
- Bundled DLLs: Moq.Analyzers.dll, Moq.CodeFixes.dll, Microsoft.CodeAnalysis.AnalyzerUtilities.dll

## Project Phases

### Phase 1: Creation (2017-11 to 2022-01) by @Litee

- VS Analyzer + VSIX project
- 9 releases (v0.0.1 through v0.0.9) over ~4 years
- Core analyzers: callback signatures, sealed class, constructors, interfaces

### Phase 2: Modernization (2022-01 to 2024-06) by @rjmurillo + @MattKotsenas

- v0.1.0: Complete infrastructure overhaul
- SDK-style projects, CPM, NBGV, editorconfig, reproducible builds
- Microsoft.CodeAnalysis.Testing harness replaced custom test harness

### Phase 3: Rapid Growth (2024-06 to 2025-11)

- Ownership formally transferred from @Litee
- IOperation-based analysis adopted (ADR-007)
- v0.4.0: Largest release. 3 new analyzers, WellKnownTypes, .NET 10 SDK, 200+ PRs

### Phase 4: Stabilization (2025-11 to present)

- v0.4.1: Fixed critical CS8032 regression from v0.4.0
- Focus on false positive elimination
- AI-assisted development tooling onboarded

## Critical Lessons from Releases

### CS8032 Assembly Mismatch (v0.4.0 -> v0.4.1)

Transitive dependency on System.Collections.Immutable caused version conflicts with .NET 8 SDK hosts.
Fixed by downgrading pins and adding CI load tests.
Lesson: Analyzer NuGet packages MUST validate against multiple SDK host versions before release.

### Missing AnalyzerUtilities DLL (v0.3.0-alpha)

AnalyzerUtilities.dll was missing from the NuGet package, causing IDE crashes.
Fixed in v0.3.0-alpha.1.
Lesson: NuGet package contents need verification tests (now covered by Verify.Xunit snapshots).

### Pre-release Channels Prevent User Impact

Alpha releases (v0.3.0-alpha, v0.4.1-alpha) caught regressions before stable users were affected.

## Breaking Changes

1. v0.1.0: Repository transferred (diagnostic IDs unchanged)
2. v0.4.0: Minimum .NET SDK 8 for development (consumers unaffected)
3. v0.4.0: CS8032 regression (fixed in v0.4.1)

## 14 Total Releases

v0.0.1 through v0.0.9 (Litee era), v0.1.0, v0.1.1, v0.1.2, v0.2.0, v0.3.0-alpha, v0.3.0-alpha.1, v0.3.0, v0.3.1, v0.4.0, v0.4.1-alpha, v0.4.1, v0.4.2
