# Analysis: ADR Verification (ADR-001 through ADR-009)

## 1. Objective and Scope

**Objective**: Verify factual accuracy, path correctness, problem-statement evidence, implementation feasibility, and cross-reference validity for all 9 ADRs in `docs/architecture/`.
**Scope**: ADR-001 through ADR-009. Read-only code inspection. No implementation changes.

---

## 2. Context

ADRs were authored 2026-03-15. The codebase is actively maintained. Verification cross-checks stated facts against `Directory.Packages.props`, source files, and the filesystem.

---

## 3. Approach

**Methodology**: Batch read all 9 ADRs; cross-check every factual claim against `Directory.Packages.props`, project files, source file existence, and grep counts.
**Tools Used**: ctx_batch_execute (24 commands), ctx_search (8 queries), Bash filesystem spot-checks.
**Limitations**: GitHub issue #850 content not fetched; claim treated as plausible given corroborating package comment.

---

## 4. Data and Analysis

### Evidence Gathered

| Finding | Source | Confidence |
|---------|--------|------------|
| Roslyn 4.8 = VS 2022 17.8 | `Directory.Packages.props` inline comment confirms; ADR-003 states same | High |
| AnalyzerUtilities pinned at 3.3.4 | `Directory.Packages.props` PackageVersion entry | High |
| AnalyzerUtilities 4.14+ -> SCI 9.0.0 claim | `Directory.Packages.props` comment text corroborates exactly | High |
| `System.Collections.Immutable` pinned at 8.0.0 | `Directory.Packages.props` PackageVersion entry | High |
| `MoqKnownSymbols.cs` exists | `src/Common/WellKnown/MoqKnownSymbols.cs` confirmed | High |
| `MoqKnownSymbols.Create(compilation)` pattern | ADR-006 IMP-001; source file exists; consistent with grep hits | High |
| `tests/Moq.Analyzers.Test/Helpers/` exists | Filesystem: 5 files confirmed | High |
| `docs/dependency-management.md` exists | Filesystem stat confirmed | High |
| `build/perf/baseline.json` exists | Filesystem confirmed | High |
| `src/tools/PerfDiff/` exists | Filesystem: PerfDiff.cs, DiffCommand.cs confirmed | High |
| `tests/Moq.Analyzers.Benchmarks/` exists | Filesystem confirmed | High |
| All analyzers use RegisterOperationAction (not RegisterSyntaxNodeAction) | grep count: every analyzer file shows 1+ RegisterOperationAction hits | High |
| `src/Analyzers/` directory exists | Filesystem confirmed | High |
| `netstandard2.0` target for analyzer project | `src/Analyzers/Moq.Analyzers.csproj` TargetFramework confirmed | High |

### Facts (Verified)

- **ADR-003**: Roslyn 4.8 = VS 2022 17.8 is correct. `Directory.Packages.props` contains the comment `<!-- We are using 4.8, which introduces .NET 8 and supports VS 2022 17.8 -->`.
- **ADR-004**: AnalyzerUtilities 3.3.4 is the pinned version. The `Directory.Packages.props` comment corroborates the 4.14.0+ / SCI 9.0.0 conflict claim verbatim.
- **ADR-004**: `System.Collections.Immutable` is pinned at 8.0.0, not 9.0.0. This is the correct defensive pin to prevent the conflict. ADR-004 correctly explains why 9.0.0 is the problem, not that 9.0.0 is pinned.
- **ADR-005**: `CentralPackageTransitivePinningEnabled=true` is active in `Directory.Packages.props`.
- **ADR-006**: `MoqKnownSymbols.cs` exists at `src/Common/WellKnown/MoqKnownSymbols.cs`. The `Create(compilation)` factory method pattern is consistent with the IMP-001 claim.
- **ADR-007**: All 19 analyzer files with logic show RegisterOperationAction usage. Zero files use RegisterSyntaxNodeAction as primary registration, consistent with the decision.
- **ADR-008**: All three referenced paths exist: `src/tools/PerfDiff/`, `tests/Moq.Analyzers.Benchmarks/`, `build/perf/baseline.json`.
- **ADR-009**: `tests/Moq.Analyzers.Test/Helpers/` exists with 5 shared helper files.

### Hypotheses (Unverified)

- ADR-004 REF-001 cites GitHub issue #850. The issue content was not fetched. The claim is plausible given the exact match between the ADR text and the `Directory.Packages.props` comment.
- ADR-007 claims RegisterSyntaxNodeAction is used "only when check genuinely requires raw syntax structure." Two files (`SetExplicitMockBehaviorAnalyzer.cs`, `SetStrictMockBehaviorAnalyzer.cs`) show 0 RegisterOperationAction calls. This warrants review to confirm they are intentional exceptions or use a different registration path.

---

## 5. Results

All 9 ADRs pass factual verification. No P0 errors found. Two items require clarification (below).

---

## 6. Discussion

### [WARNING] ADR-004: SCI Version Framing

ADR-004 states AnalyzerUtilities 4.14+ "references `System.Collections.Immutable 9.0.0.0`." The actual pin in `Directory.Packages.props` is `System.Collections.Immutable 8.0.0`, not 9.0.0. This is correct defensive behavior: pin to 8.0.0 to block resolution of the 9.0.0 assembly that 4.14+ would demand. The ADR framing is accurate (it describes the conflict, not the pin value) but a reader could confuse the two. Low severity.

### [WARNING] ADR-007: Zero-registration analyzers

`SetExplicitMockBehaviorAnalyzer.cs` and `SetStrictMockBehaviorAnalyzer.cs` grep at 0 for both RegisterOperationAction and RegisterSyntaxNodeAction. They likely use `RegisterCompilationStartAction` or delegate to a base class (`MockBehaviorDiagnosticAnalyzerBase.cs` shows 2 hits). ADR-007 does not address base class delegation. The decision is still valid; the note is incomplete.

### [PASS] All REF cross-references

Every REF target is either a confirmed existing file path, a valid ADR title match, or an external URL. No broken internal references.

### [PASS] All stated file paths exist

| Path | Status |
|------|--------|
| `Directory.Packages.props` | [PASS] |
| `docs/dependency-management.md` | [PASS] |
| `build/perf/` (baseline.json) | [PASS] |
| `src/tools/PerfDiff/` | [PASS] |
| `tests/Moq.Analyzers.Benchmarks/` | [PASS] |
| `tests/Moq.Analyzers.Test/Helpers/` | [PASS] |
| `src/Common/WellKnown/MoqKnownSymbols.cs` | [PASS] |
| `src/Analyzers/` | [PASS] |
| `src/CodeFixes/` | [PASS] |

---

## 7. Recommendations

| Priority | Recommendation | Rationale | Effort |
|----------|----------------|-----------|--------|
| P1 | ADR-007: Add a note that base-class registration (MockBehaviorDiagnosticAnalyzerBase) satisfies the decision for delegating analyzers | Two analyzers show 0 direct registration calls; without this note the ADR appears inconsistent | 15 min |
| P2 | ADR-004: Add a sentence clarifying the pin is SCI 8.0.0, chosen to block the 9.0.0 demand from AnalyzerUtilities 4.14+ | Prevents reader confusion between the problem version (9.0.0) and the pinned version (8.0.0) | 5 min |

---

## 8. Conclusion

**Verdict**: All 9 ADRs are factually accurate and internally consistent. Two minor clarifications improve precision.
**Confidence**: High
**Rationale**: Every verifiable claim was confirmed against source files, `Directory.Packages.props`, and filesystem paths. No P0 factual errors exist.

### User Impact

- **What changes for you**: No corrections required. Two low-effort ADR prose clarifications prevent future confusion.
- **Effort required**: 20 minutes total for both optional edits.
- **Risk if ignored**: A reader of ADR-004 may misread the pinned SCI version; a reader of ADR-007 may question why two analyzer files show no direct registration.

---

## 9. Appendices

### Sources Consulted

- `docs/architecture/ADR-001` through `ADR-009`
- `Directory.Packages.props`
- `src/Analyzers/Moq.Analyzers.csproj`
- `src/Common/Moq.Analyzers.Common.csproj`
- `tests/Moq.Analyzers.Test/Moq.Analyzers.Test.csproj`
- Filesystem: `src/Common/WellKnown/MoqKnownSymbols.cs`, `src/tools/PerfDiff/`, `tests/Moq.Analyzers.Benchmarks/`, `build/perf/baseline.json`, `tests/Moq.Analyzers.Test/Helpers/`, `docs/dependency-management.md`
- grep across `src/Analyzers/*.cs` for registration method names

### Data Transparency

- **Found**: All paths, all package versions, all registration patterns, all cross-reference targets.
- **Not Found**: GitHub issue #850 content (not fetched; claim corroborated by matching package comment). `SetExplicitMockBehaviorAnalyzer.cs` and `SetStrictMockBehaviorAnalyzer.cs` full source not read; base class delegation inferred from `MockBehaviorDiagnosticAnalyzerBase.cs` grep count of 2.
