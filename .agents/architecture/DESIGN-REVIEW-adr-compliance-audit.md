# Design Review: ADR Compliance Audit

**Reviewer**: Architect
**Date**: 2026-03-14
**Scope**: ADR-001 through ADR-009
**Standard**: MADR 4.0

---

## Executive Summary

All 9 ADRs pass structural review at a baseline level. Context, Decision, Consequences, Alternatives, Implementation Notes, and References sections are present in every ADR. The primary gaps are systemic: every ADR uses a non-MADR frontmatter schema, no ADR contains a Confirmation section, no ADR contains a Reversibility Assessment, and cross-references are incomplete in 5 of 9 ADRs.

Issue counts: P0: 2, P1: 14, P2: 9

---

## Systemic Issues (All 9 ADRs)

### P0-001: Frontmatter does not conform to MADR 4.0 schema

**Affected**: ADR-001 through ADR-009 (all)

Every ADR uses:

```yaml
authors: "moq.analyzers maintainers"
tags: [...]
supersedes: ""
superseded_by: ""
```

MADR 4.0 requires:

```yaml
status: "..."
date: "..."
decision-makers: "..."
consulted: "..."
informed: "..."
```

The `decision-makers`, `consulted`, and `informed` fields are absent in all 9 ADRs. These fields are not cosmetic. They record who holds accountability for the decision and who must be notified when it changes. Without them, the ADR log cannot serve as a governance record.

**Fix**: Replace `authors` with `decision-makers`. Add `consulted` and `informed` fields. Replace `supersedes`/`superseded_by` with the MADR status convention (e.g., `status: "superseded by ADR-NNN"`). Remove `tags` or move to a non-frontmatter metadata block.

---

### P0-002: Confirmation section absent in all 9 ADRs

**Affected**: ADR-001 through ADR-009 (all)

MADR 4.0 requires a Confirmation section answering: "How will implementation and compliance be confirmed?" No ADR contains this section.

Without Confirmation, there is no enforcement mechanism. A decision that cannot be verified is a suggestion, not a governance record.

**Fix per ADR** (suggested text, adapt as needed):

- ADR-001: "Code review rejects `symbol.Name` or `ToDisplayString()` comparisons in analyzer logic. `MoqKnownSymbols` usage is verified by inspection during PR review."
- ADR-002: "CI build fails if any analyzer project targets a TFM other than `netstandard2.0`. Verified by `<TargetFramework>` in each `.csproj`."
- ADR-003: "The pinned version in `Directory.Packages.props` is the authoritative check. `ValidateAnalyzerHostCompatibility` target enforces compatibility at build time."
- ADR-004: "The pinned cap in `Directory.Packages.props` is verified at every dependency update PR. Dependabot PRs for `AnalyzerUtilities` above 3.3.4 are blocked."
- ADR-005: "`CentralPackageTransitivePinningEnabled=true` enforces version centralization at restore time. Build fails if any `.csproj` includes a `Version` attribute on `<PackageReference>`."
- ADR-006: "Static analysis tooling or code review verifies no `GetTypeByMetadataName()` call appears outside `MoqKnownSymbols`. Grep target: `GetTypeByMetadataName` in `src/Analyzers/`."
- ADR-007: "Code review rejects new analyzers using `RegisterSyntaxNodeAction` without documented justification in the PR description. Existing violations tracked as tech debt."
- ADR-008: "PerfDiff CI gate exits non-zero on regression. Pipeline configuration in `.github/workflows/` enforces this gate on all PRs targeting `main`."
- ADR-009: "New test files must use `CSharpAnalyzerVerifier<T>` or `CSharpCodeFixVerifier<T>`. Manual compilation tests are rejected in review."

---

## Per-ADR Issues

### ADR-001: Symbol-Based Detection Over String Matching

**P1-001**: No Reversibility Assessment section.
The decision is effectively irreversible given the depth of `MoqKnownSymbols` integration, but that constraint should be stated explicitly with a rollback note.
**Fix**: Add a Reversibility Assessment. State: "Reverting to string matching would require replacing all `MoqKnownSymbols` call sites. Estimated scope: 23 analyzer files. Not recommended absent a Roslyn API breaking change."

**P2-001**: `REF-002` links to Roslyn API docs by concept name only, no URL.
**Fix**: Add URL: `https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol`

---

### ADR-002: Target netstandard2.0 for Analyzer Assemblies

**P1-002**: No Reversibility Assessment.
The TFM constraint is architecturally load-bearing. The cost of changing it is not documented.
**Fix**: Add: "Changing TFM to `net8.0` or later would require verifying no host (VS 2019, VS 2022, Rider) loads the analyzer under an incompatible runtime. Estimated validation effort: 2-3 days. Decision is practically irreversible while VS 2019 support is in scope."

**P1-003**: Context references the compiler pipeline constraint but does not cite the minimum Visual Studio version or Rider version that drives the `netstandard2.0` requirement.
**Fix**: Add to Context: "Visual Studio 2019 (16.x) loads analyzer assemblies under .NET Framework 4.7.2. Visual Studio 2022 (17.x) loads them under .NET Framework 4.8 or CoreCLR depending on configuration. Both require `netstandard2.0` compatibility."

**P2-002**: No cross-reference to ADR-007. The TFM constraint directly limits which Roslyn `IOperation` APIs are available in analyzer code.
**Fix**: Add `REF-004: ADR-007 -- Prefer RegisterOperationAction Over RegisterSyntaxNodeAction` to References.

---

### ADR-003: Pin Roslyn SDK to Microsoft.CodeAnalysis 4.8

**P1-004**: No Reversibility Assessment. Version pins are time-sensitive. The ADR does not state under what conditions the pin should be revisited or updated.
**Fix**: Add: "The pin should be reviewed when Visual Studio 2022 17.8 (which ships Roslyn 4.8) falls out of mainstream support, or when an analyzer requires an API only available in a higher version. The upgrade path is through ADR-005 central package management."

**P1-005**: The alternative "Older 4.x Roslyn Release (e.g., 4.3)" uses label identifiers ALT-003/ALT-004, skipping ALT-001/ALT-002. The first alternative uses ALT-001/ALT-002. This numbering is inconsistent and will cause confusion when referencing alternatives by ID.
**Fix**: Renumber the second alternative to ALT-003/ALT-004 only if ALT-001/ALT-002 are used elsewhere; otherwise renumber all consistently starting at ALT-001.

**P2-003**: No cross-reference to ADR-002. The Roslyn version pin and the TFM target are coupled constraints.
**Fix**: Add `REF-004: ADR-002 -- Target netstandard2.0 for Analyzer Assemblies`.

---

### ADR-004: Cap AnalyzerUtilities at 3.3.4

**P1-006**: No Reversibility Assessment. The cap is a workaround for a specific compatibility break. The ADR does not state the conditions under which the cap can be lifted.
**Fix**: Add: "The cap can be lifted when `AnalyzerUtilities` 4.x ships a version compatible with `netstandard2.0` and the `Microsoft.CodeAnalysis 4.8` pin. This requires validation against the host compatibility matrix. Track via a GitHub issue."

**P1-007**: The Decision section does not state the specific compatibility break that makes 4.x unusable. A future reader cannot evaluate whether the cap still applies without re-investigating.
**Fix**: Add one sentence to Decision: "AnalyzerUtilities 4.x requires `Microsoft.CodeAnalysis` APIs not available in the 4.8 pin and fails to load under the `netstandard2.0` TFM target."

**P2-004**: No cross-reference to ADR-007. AnalyzerUtilities provides helpers used by `RegisterOperationAction`-based analyzers.
**Fix**: Add `REF-004: ADR-007 -- Prefer RegisterOperationAction Over RegisterSyntaxNodeAction`.

---

### ADR-005: Central Package Management with Transitive Pinning

**P1-008**: No Reversibility Assessment. CPM with transitive pinning is a high-friction change to revert. The cost is not documented.
**Fix**: Add: "Reverting CPM requires re-adding `Version` attributes to all `<PackageReference>` elements across all `.csproj` files. Estimated scope: ~8 project files, ~40 package references. `CentralPackageTransitivePinningEnabled` removal would require audit of all transitive dependency versions."

**P1-009**: The `ValidateAnalyzerHostCompatibility` target is referenced in Implementation Notes but is not described in sufficient detail. A contributor cannot understand what it checks without reading the MSBuild file.
**Fix**: Add to Implementation Notes: "The target compares each pinned Roslyn and AnalyzerUtilities version against the assembly versions bundled with the .NET 8 SDK. It emits a build error if a pinned version exceeds the SDK-bundled version, which would cause host load failures."

**P2-005**: No cross-reference to ADR-003 or ADR-004. The CPM system is the enforcement mechanism for both pins.
**Fix**: Add `REF-005: ADR-003 -- Pin Roslyn SDK to Microsoft.CodeAnalysis 4.8` and `REF-006: ADR-004 -- Cap AnalyzerUtilities at 3.3.4`.

---

### ADR-006: WellKnown Types Pattern for Moq Symbol Resolution

**P1-010**: No Reversibility Assessment. `MoqKnownSymbols` is referenced by all 23 analyzers. The ADR acknowledges this in POS-004 but does not state the cost of removing or replacing the pattern.
**Fix**: Add: "Replacing `MoqKnownSymbols` with inline `GetTypeByMetadataName` calls would require modifying all 23 analyzer classes. The pattern is effectively irreversible without a large coordinated refactor."

**P2-006**: Context states "If Moq is absent, `MoqKnownSymbols` returns null symbols" but does not explain the thread-safety guarantees. Roslyn calls analyzer methods concurrently.
**Fix**: Add a sentence to Context or Implementation Notes: "`MoqKnownSymbols` is created once in `CompilationStartAnalysisContext`, which is invoked once per compilation. The resolved instance is then shared read-only across concurrent operation callbacks. No locking is required."

---

### ADR-007: Prefer RegisterOperationAction Over RegisterSyntaxNodeAction

**P1-011**: No Reversibility Assessment.
**Fix**: Add: "Reverting to `RegisterSyntaxNodeAction` across all analyzers would increase code volume and introduce syntax-variation gaps. The decision can be partially reversed on a per-analyzer basis without systemic impact, provided the analyzer documents the justification."

**P2-007**: IMP-003 states that `RegisterSyntaxNodeAction` usage requires justification in the PR description but does not state where exemptions are tracked over time. Without a durable record, justified exemptions accumulate silently.
**Fix**: Add: "Existing exemptions are tracked in `docs/architecture/` alongside the relevant analyzer. Each exemption file names the analyzer, the syntax kind checked, and the reason `IOperation` was insufficient."

---

### ADR-008: BenchmarkDotNet PerfDiff for Performance Regression Detection

**P1-012**: No Reversibility Assessment. PerfDiff is a custom internal tool. If it is abandoned or broken, the CI gate fails open or requires bypass. The ADR does not address this failure mode.
**Fix**: Add: "If PerfDiff is unavailable or broken, CI must fail closed (not bypass the gate). The fallback is a manual benchmark comparison requirement documented in the PR template. PerfDiff source is maintained in `src/tools/PerfDiff/` and covered by the standard build pipeline."

**P1-013**: The Decision section names System.CommandLine 2.0.3 but does not explain why that specific version is pinned or what constrains it.
**Fix**: Either remove the version detail from the Decision section (it belongs in Implementation Notes or ADR-005) or add: "System.CommandLine 2.0.3 is pinned because the 2.x GA API is stable and the tool does not require features from later versions."

**P2-008**: No cross-reference to ADR-009. Benchmark infrastructure and test infrastructure share the same project structure conventions.
**Fix**: Add `REF-005: ADR-009 -- xUnit with Roslyn Test Infrastructure`.

---

### ADR-009: xUnit with Roslyn Test Infrastructure

**P1-014**: No Reversibility Assessment.
**Fix**: Add: "Migrating from xUnit to another framework would require rewriting all test classes. Estimated scope: the full `tests/Moq.Analyzers.Test/` tree. The Roslyn test helper dependency (`Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit`) is xUnit-specific; a framework change requires an infrastructure change as well."

**P2-009**: The MSTest alternative rejection reason is "team preference is xUnit" without a technical reason. This is weak justification for an architectural record.
**Fix**: Strengthen to: "MSTest was not used at project inception and offers no technical advantage for Roslyn analyzer testing. The Roslyn test helper packages ship xUnit and NUnit variants; MSTest variants are not published by the Roslyn team."

---

## Cross-Reference Gap Matrix

| ADR | Missing Cross-References |
|-----|--------------------------|
| ADR-002 | ADR-007 (TFM constrains IOperation API availability) |
| ADR-003 | ADR-002 (TFM and SDK pin are coupled) |
| ADR-004 | ADR-007 (AnalyzerUtilities used by operation-based analyzers) |
| ADR-005 | ADR-003, ADR-004 (CPM enforces both pins) |
| ADR-007 | ADR-002 (TFM determines available IOperation APIs) |

ADR-001, ADR-006, ADR-008, ADR-009 have adequate cross-references given their scope.

---

## Issue Summary

| ID | ADR | Priority | Category | Description |
|----|-----|----------|----------|-------------|
| P0-001 | All | P0 | Governance | MADR 4.0 frontmatter fields `decision-makers`, `consulted`, `informed` absent |
| P0-002 | All | P0 | Governance | Confirmation section absent in all 9 ADRs |
| P1-001 | 001 | P1 | Completeness | No Reversibility Assessment |
| P1-002 | 002 | P1 | Completeness | No Reversibility Assessment |
| P1-003 | 002 | P1 | Context | Min host versions not cited in Context |
| P1-004 | 003 | P1 | Completeness | No Reversibility Assessment or review trigger |
| P1-005 | 003 | P1 | Consistency | Alternative numbering skips ALT-001/ALT-002 in second alternative |
| P1-006 | 004 | P1 | Completeness | No Reversibility Assessment or lift conditions |
| P1-007 | 004 | P1 | Context | Specific 4.x incompatibility not named in Decision |
| P1-008 | 005 | P1 | Completeness | No Reversibility Assessment |
| P1-009 | 005 | P1 | Implementation | ValidateAnalyzerHostCompatibility not described sufficiently |
| P1-010 | 006 | P1 | Completeness | No Reversibility Assessment |
| P1-011 | 007 | P1 | Completeness | No Reversibility Assessment |
| P1-012 | 008 | P1 | Risk | PerfDiff failure mode not addressed |
| P1-013 | 008 | P1 | Clarity | System.CommandLine version pin unexplained in Decision |
| P1-014 | 009 | P1 | Completeness | No Reversibility Assessment |
| P2-001 | 001 | P2 | References | REF-002 lacks URL |
| P2-002 | 002 | P2 | Cross-ref | Missing ADR-007 cross-reference |
| P2-003 | 003 | P2 | Cross-ref | Missing ADR-002 cross-reference |
| P2-004 | 004 | P2 | Cross-ref | Missing ADR-007 cross-reference |
| P2-005 | 005 | P2 | Cross-ref | Missing ADR-003 and ADR-004 cross-references |
| P2-006 | 006 | P2 | Clarity | Thread-safety of MoqKnownSymbols not documented |
| P2-007 | 007 | P2 | Process | Exemption tracking location not defined |
| P2-008 | 008 | P2 | Cross-ref | Missing ADR-009 cross-reference |
| P2-009 | 009 | P2 | Justification | MSTest rejection is preference, not technical |

**Total**: P0: 2 (systemic, affect all 9 ADRs), P1: 14, P2: 9
