---
name: moq-analyzers-rule-lifecycle
description: Walks through adding, changing, and shipping an analyzer rule in moq.analyzers end-to-end. Load when the task is "add a new Moq1XXX rule", "add a code fix", "change a shipped rule's severity/category", "update AnalyzerReleases", "prepare/publish a release", "why does RS2000 fail my build", "what files does a new analyzer need", or picking a diagnostic ID/category/severity. Do NOT load for analyzer implementation techniques (use roslyn-analyzer-reference and moq-analyzers-architecture-contract), test-quality bar details (moq-analyzers-validation-and-qa), build/environment setup (moq-analyzers-build-and-env), or PR-gate mechanics (moq-analyzers-change-control).
---

# Rule Lifecycle: Add, Change, Ship

This is the run-and-operate manual for a rule's whole life: new rule → shipped rule change → release promotion → (retirement: policy gap, see below). It tells you WHICH files to touch and in WHAT format. It does not teach you how to write the analyzer logic itself — that is `roslyn-analyzer-reference` (Roslyn concepts) and `moq-analyzers-architecture-contract` (this repo's invariants).

Definitions used throughout:

- **Roslyn analyzer**: a class that plugs into the C# compiler and reports **diagnostics** (warnings/errors like `Moq1600`) as code compiles.
- **Diagnostic descriptor**: the static metadata object (`DiagnosticDescriptor`) declaring a rule's ID, title, message, category, and severity.
- **Release tracking (RS2000 family)**: the `Microsoft.CodeAnalysis.Analyzers` package (pinned 5.3.0 in `build/targets/codeanalysis/Packages.props`, 2026-07-02) validates that every descriptor ID is declared in `AnalyzerReleases.Shipped.md` or `AnalyzerReleases.Unshipped.md`. A missing entry raises RS2000. Under the CI-parity build (`dotnet build /p:PedanticMode=true`, which sets `TreatWarningsAsErrors` — see `build/targets/codeanalysis/CodeAnalysis.targets:3-5`) any RS2xxx **fails the build**. A lenient local `dotnet build` may show it only as a warning — treat every RS2xxx as a build blocker.
- **NBGV**: Nerdbank.GitVersioning. Computes the package version from `version.json` + branch name + git height. No version numbers are hand-edited into csproj files.

All commands are repo-root relative and assume `export PATH="$HOME/.dotnet:$PATH"`.

## Part 1 — Adding a new rule: the file checklist

Ground truth: commit `3f98710` ("feat: add Moq1600 protected member setup matcher validation (#1088)") is the most recent complete new-rule PR. It touched exactly these files (10 items; the classic "8-file checklist" plus the root README row and the category-pinning test). Copy this table into your working notes and check every row:

| # | File | What you add | Skip allowed? |
|---|------|--------------|---------------|
| 1 | `src/Common/DiagnosticIds.cs` | `internal const string YourRuleName = "MoqXXXX";` | Never |
| 2 | `src/Analyzers/<Name>Analyzer.cs` | The analyzer (requirements below) | Never |
| 3 | `src/Analyzers/AnalyzerReleases.Unshipped.md` | One `### New Rules` row (format below) | Never — RS2000 breaks CI build |
| 4 | `docs/rules/MoqXXXX.md` | Rule doc from the house template (below) | Never |
| 5 | `docs/rules/README.md` | Row in the master table + keep the ID-range table honest | Never |
| 6 | `README.md` (repo root) | Row in the root rule table (see `README.md:42` for Moq1600's) | Never |
| 7 | `tests/Moq.Analyzers.Test/<Name>AnalyzerTests.cs` | Diagnostic + no-diagnostic + Doppelganger tests | Never |
| 8 | `tests/Moq.Analyzers.Test/DiagnosticCategoryTests.cs` | One row in the matching `TheoryData` (pins ID + category) | Never |
| 9 | `tests/Moq.Analyzers.Benchmarks/MoqXXXX...Benchmarks.cs` | Per-rule benchmark file (convention below) | Never |
| 10 | `src/CodeFixes/<Name>Fixer.cs` + fixer tests + `src/Common/WellKnown/MoqKnownSymbols.cs` additions | Only if shipping a code fix / new well-known symbols | Optional parts only |

If your diff for a new rule touches fewer than rows 1–9, something is missing. `AllAnalyzersVerifier` enrollment (below) is automatic — there is no file to edit for it, but there IS a trap.

### 1. Diagnostic ID — `src/Common/DiagnosticIds.cs`

Pick the next free ID in the correct range. The authoritative range table lives in `docs/rules/README.md` ("Diagnostic ID Ranges"):

| Range | Category | Meaning |
|-------|----------|---------|
| Moq1000–1099 | Usage | Correct use of Moq APIs (sealed mocks, As&lt;T&gt;, …) |
| Moq1100–1199 | Correctness | Callback signatures, setup validity |
| Moq1200–1299 | Correctness | Async result setups, constructor args |
| Moq1300–1399 | Usage | Literals, API usage patterns |
| Moq1400–1499 | Best Practice | Explicit/strict mock behavior |
| Moq1500–1599 | Best Practice | Repository and verification patterns |
| Moq1600–1699 | Usage | Protected member setup/verification patterns |
| Moq1700–1999 | Reserved | Future rules |

Constraints (verified against `src/Common/DiagnosticIds.cs`, 2026-07-02):

- `Moq1209` is intentionally reserved and unassigned — do not take it.
- `Moq1003` was removed in Release 0.0.6 and later **reused** for `InternalTypeMustHaveInternalsVisibleToAnalyzer` (currently unshipped). That reuse is historical fact, not license: general Roslyn convention is to never reuse a retired ID. Prefer a fresh ID; if you believe reuse is warranted, raise it with the maintainer first.
- One analyzer class may own several IDs (`ConstructorArgumentsShouldMatchAnalyzer` owns Moq1001 and Moq1002).

### 2. The analyzer file — `src/Analyzers/<Name>Analyzer.cs`

Hard requirements (each one is load-bearing; violating any produces a silent or delayed failure):

| Requirement | Why |
|-------------|-----|
| `namespace Moq.Analyzers;` — exactly | `AllAnalyzersVerifier` (tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs) reflection-discovers analyzers by `type.Namespace == "Moq.Analyzers"`. A different namespace **silently drops your analyzer from every no-false-positive suite** — tests stay green while coverage vanishes. |
| `[DiagnosticAnalyzer(LanguageNames.CSharp)]` on the class | Registers with the compiler; also part of the discovery predicate above. |
| Category from `src/Common/DiagnosticCategory.cs` | Only three legal values: `DiagnosticCategory.Usage` ("Usage"), `.Correctness` ("Correctness"), `.BestPractice` ("Best Practice"). Never a raw string. |
| `helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.YourRule}.md"` | The commit-pinned pattern used by all 25 descriptors (verify: `grep -c GitCommitId src/Analyzers/*.cs` → 25 as of 2026-07-02). Guarantees the help link matches the shipped bits, not `main`. |
| `isEnabledByDefault: true` | All 25 current descriptors are enabled by default (2026-07-02). A disabled-by-default rule would be a policy decision — ask the maintainer. |
| `Initialize` boilerplate: `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`, `EnableConcurrentExecution()`, `RegisterCompilationStartAction` → build `MoqKnownSymbols` once → `IsMockReferenced()` early exit | Repo doctrine; details and rationale in `moq-analyzers-architecture-contract`. |

Reference implementation to copy structure from: `src/Analyzers/ProtectedSetupShouldUseItExprAnalyzer.cs` (Moq1600) — descriptor at the top, compilation-start symbol resolution with null guards, `RegisterOperationAction(OperationKind.Invocation)`, symbol-equality checks only.

### 3. Release-tracking row — `src/Analyzers/AnalyzerReleases.Unshipped.md`

Exact row format, copied from the live file (2026-07-02):

```text
### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1600 | Usage | Warning | ProtectedSetupShouldUseItExprAnalyzer
```

- `Category` and `Severity` must match the descriptor exactly (the RS2xxx analyzers cross-check them).
- `Notes` = the analyzer class name. (Shipped entries historically also append a `[Documentation](...)` link; unshipped entries currently use the bare class name.)
- Forgetting this row → RS2000 → CI-parity build failure. Reproduce locally with `dotnet build /p:PedanticMode=true`.
- **Never touch `src/Analyzers/AnalyzerReleases.Shipped.md` for a new rule.** See Part 3.

### 4. Rule documentation — `docs/rules/MoqXXXX.md`

There is no separate template file; the house template is the structure every existing rule doc follows. Use `docs/rules/Moq1600.md` as the model. Required sections, in order:

1. H1: `# MoqXXXX: <title>`
2. Metadata table: `| Item | Value |` with rows `Enabled` / `Severity` / `CodeFix` (True/False)
3. Prose: what the rule detects and **why it matters at runtime** (Moq1600's doc explains that `It` matchers in string-based protected setups compile but fail at runtime)
4. `## Examples of patterns that are flagged by this analyzer` — compiling C# with a `// MoqXXXX: <reason>` comment on the flagged line
5. `## Solution` — the corrected code
6. Optional `## When this rule does not apply` — patterns that look similar but are legal (strongly recommended; this is your false-positive contract in prose)
7. `## Suppress a warning` — the standard `#pragma warning disable MoqXXXX` block and the `.editorconfig` `dotnet_diagnostic.MoqXXXX.severity = none` block, with links to Microsoft configuration docs

Example code in docs must actually compile against Moq — plausible-but-wrong doc snippets are treated as bugs here. See `moq-analyzers-docs-and-writing` for prose style.

### 5–6. Index rows — `docs/rules/README.md` and root `README.md`

- `docs/rules/README.md`: add a row `| [MoqXXXX](./MoqXXXX.md) | <Category> | <Title> | [<File>.cs](../../src/Analyzers/<File>.cs) |` in ID order.
- Root `README.md`: add the matching row to the rule table (Moq1600's is at `README.md:42`).

### 7. Tests — `tests/Moq.Analyzers.Test/<Name>AnalyzerTests.cs`

The full test-quality bar lives in `moq-analyzers-validation-and-qa`; the lifecycle-relevant minimum, verified against `ProtectedSetupShouldUseItExprAnalyzerTests.cs`:

- `[Theory]` + `[MemberData]` + `public static IEnumerable<object[]> ...Data()` returning `new object[][] { ... }` chained with `.WithNamespaces().WithMoqReferenceAssemblyGroups()` (fans each case across namespace styles × Moq 4.8.2 and 4.18.4). This is a MANDATORY pattern per `CONTRIBUTING.md:444` for code fixes and the de facto pattern for analyzer tests.
- Diagnostic assertions use markup: `{|Moq1600:It.IsAny<string>()|}` — asserts ID **and exact span**. Unmarked source in a test is a genuine no-diagnostic assertion.
- If a scenario only exists in newer Moq, split it into a separate data source chained with `.WithNewMoqReferenceAssemblyGroups()` (see `NewMoqOnlyDiagnosticData` in the Moq1600 tests).
- A `ShouldNotTriggerWhenMoqNotReferenced` fact using `ReferenceAssemblyCatalog.Net80` (no Moq reference → analyzer must early-exit).
- A **Doppelganger test**: a user-defined type that mimics the Moq API shape (e.g., a class with its own `Setup(string, object)` method, or a hand-rolled `Mock<T>`/`MockBehavior`) must NOT trigger. Use `DoppelgangerTestHelper` (tests/Moq.Analyzers.Test/Helpers/DoppelgangerTestHelper.cs) for Mock&lt;T&gt; look-alikes, or an inline doppelganger like `ShouldNotDiagnoseUserDefinedProtectedClassWithSetup` in the Moq1600 tests. This pins ADR-001 (symbol-based detection, never name matching).
- Test code must be valid, compiling C# — never write setups for static/const/sealed members that would not compile (`CONTRIBUTING.md:580`).

### 8. Category pin — `tests/Moq.Analyzers.Test/DiagnosticCategoryTests.cs`

Add one row to the `TheoryData` matching your category, e.g. (line 21, 2026-07-02):

```csharp
{ new ProtectedSetupShouldUseItExprAnalyzer(), "Moq1600", DiagnosticCategory.Usage },
```

This is the executable version of the ID-range table: it fails if your ID and category drift apart.

### AllAnalyzersVerifier — automatic, with one trap

`AllAnalyzersVerifier.VerifyAllAnalyzersAsync(source, group)` reflection-discovers every concrete `[DiagnosticAnalyzer]` in namespace `Moq.Analyzers` and asserts a source sample triggers NONE of them. Your new analyzer is enrolled automatically — **if and only if the namespace is exactly `Moq.Analyzers`**. There is no compile error and no test failure for a wrong namespace; the analyzer simply stops being checked. Verify enrollment once:

```bash
dotnet test --settings ./build/targets/tests/test.runsettings --filter "FullyQualifiedName~AllAnalyzers"
```

and confirm your new analyzer appears in test output, or temporarily make your analyzer fire on a known-clean sample and watch the suite fail.

### 9. Benchmark — `tests/Moq.Analyzers.Benchmarks/MoqXXXX<ShortName>Benchmarks.cs`

The convention is one benchmark file per rule, and every recent rule PR (including Moq1600) added one. Coverage today: 18 `Moq1*Benchmarks.cs` files against 25 rule IDs (2026-07-02) — Moq1001, Moq1003, Moq1004, Moq1205, Moq1207, Moq1302, and Moq1420 have no dedicated file (pre-existing gap; do not use it as license to skip yours). Naming: `Moq1600Benchmarks.cs` or `Moq1203MethodSetupReturnValueBenchmarks.cs` — ID prefix mandatory, descriptive middle optional. Copy the shape of `Moq1600Benchmarks.cs`:

- Class attributes: `[InProcess]`, `[MemoryDiagnoser]`, `[BenchmarkCategory("MoqXXXX")]`
- `[Params(1, 1_000)] public int FileCount` — the PR perf gate's fast path filters on `'*(FileCount: 1)'`, so the `1` param is what gates your PR; nightly runs everything
- `[IterationSetup]` builds `FileCount` source files: each contains one triggering pattern plus one near-miss ("looks similar but does not match")
- Two `[Benchmark]` methods: `MoqXXXXWithDiagnostics` (asserts diagnostic count == `FileCount`) and `MoqXXXXBaseline` (`Baseline = true`, asserts zero diagnostics). The internal assertions make a broken benchmark fail loudly instead of measuring nothing.

The perf gate itself (PerfDiff, baseline.json, `FORCE_PERF_BASELINE`) is covered in `moq-analyzers-diagnostics-and-tooling` and `moq-analyzers-change-control`.

### 10. Optional: code fix + KnownSymbols additions

**Code fix** (`src/CodeFixes/<Name>Fixer.cs`, namespace `Moq.CodeFixes`):

- `[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(YourFixer))]` + `[Shared]`, `sealed`, inherits `CodeFixProvider`
- `FixableDiagnosticIds` references `DiagnosticIds.<YourConst>`; override `GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer`
- Model: `src/CodeFixes/ReturnsDelegateShouldReturnTaskFixer.cs`
- Tests use `CodeFixVerifier<Analyzer, Fixer>` with the mandatory data-driven pattern (`CONTRIBUTING.md:444`): each `object[]` row is `[brokenCode, fixedCode]`, `[Theory]` signature `(string referenceAssemblyGroup, string @namespace, string brokenCode, string fixedCode)`, a `Template` local function with `{{ns}}`/`{{code}}` placeholders, then `Verify.VerifyCodeFixAsync(originalSource, fixedSource, referenceAssemblyGroup)`. Model: `tests/Moq.Analyzers.Test/ReturnsDelegateShouldReturnTaskFixerTests.cs`.
- Update the rule doc's metadata table to `CodeFix | True`.

**KnownSymbols** (`src/Common/WellKnown/MoqKnownSymbols.cs`): if your analyzer needs a Moq type/member not yet exposed, add a lazily-resolved property (raw `Compilation.GetTypeByMetadataName` is banned via `BannedSymbols.txt`; go through the provider). Then add **resolution tests** in `tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.*.cs`, following the existing pair pattern:

- `X_WithoutMoqReference_ReturnsNull` / `ReturnsEmpty`
- `X_WithMoqReference_ReturnsNamedTypeSymbol` (asserts `NotNull`, plus name/arity)

The non-null-with-Moq test is the important one: `MoqKnownSymbols` already contains **phantom properties** (`IReturns`, `IReturns1`) for types that do not exist in any Moq version — they resolve null forever, and only a pinning test distinguishes "phantom by design" from "typo in metadata name". Confirm the real type/member exists in BOTH supported Moq versions first: `dotnet tool install -g dotnet-inspect` then e.g. `dotnet-inspect member "IReturns<TMock, TResult>" --package Moq --all` (the project's established verification tool; see `moq-api-reference` / `moq-analyzers-proof-toolkit` for usage). Known gap (2026-07-02): the Moq1600 commit added `IProtectedMock1`/`ItExpr`/`ProtectedExtension` symbols without such tests — do not copy that omission; adding them is an open candidate improvement.

### Pre-PR verification for a new rule

```bash
dotnet format --verify-no-changes
dotnet build /p:PedanticMode=true          # CI parity; catches RS2000 and all warnings-as-errors
dotnet test --settings ./build/targets/tests/test.runsettings
```

Then follow `moq-analyzers-change-control` for branch naming, PR evidence, and gates.

## Part 2 — Choosing severity and category

Current distribution across all 25 descriptors (verified `grep DiagnosticSeverity src/Analyzers/*.cs`, 2026-07-02):

| Severity | Count | Rules |
|----------|-------|-------|
| Error | 5 | Moq1200, Moq1201, Moq1207, Moq1210, Moq1300 |
| Warning | 18 | everything else |
| Info | 2 | Moq1410, Moq1420 |

Guidance derived from that distribution (heuristic, not written policy — when unsure, propose Warning and let review escalate):

- **Error** is reserved for code that is *guaranteed* to throw at Moq runtime — e.g., Moq1200 `Setup` on a non-overridable member always throws. All five Error rules are of this "cannot possibly work" class. An Error severity in a library that runs inside customers' builds is a build-breaker for them; the false-positive bar is correspondingly absolute.
- **Warning** is the default for "compiles, runs, but behaves wrongly or misleadingly" (Moq1600: matcher silently matches nothing) and for API misuse.
- **Info** is for stylistic/redundancy suggestions (Moq1410 prefer-strict, Moq1420 redundant `Times.AtLeastOnce()`).

Category: pick per the ID-range table in Part 1 — the ID range, the `DiagnosticCategory` constant, the Unshipped row, the two README tables, and the `DiagnosticCategoryTests` row must all agree. Note the descriptor category and the doc "Category" concepts are the same three strings; `Best Practice` contains a space (`DiagnosticCategory.BestPractice`).

## Part 3 — Changing a SHIPPED rule

A rule is "shipped" once it appears in `src/Analyzers/AnalyzerReleases.Shipped.md`. The repo's rules, quoted from `.github/copilot-instructions.md:552-553` (2026-07-02):

> **CRITICAL: Do not modify `AnalyzerReleases.Shipped.md`**. This file is an immutable record of past releases. All changes, including category or severity updates to existing rules, **MUST** be documented in `AnalyzerReleases.Unshipped.md`.
>
> **Analyzer Release Notes Logic:** For this repository, `AnalyzerReleases.Unshipped.md` must **only** contain a `### New Rules` section. Any modification to a previously shipped rule (e.g., changing its category or severity) is listed as if it were a new rule in this file. The `Changed Rules` and `Removed Rules` sections are only used in `AnalyzerReleases.Shipped.md` when a release is being finalized.

So: change the descriptor in code, then record the rule's NEW state in `Unshipped.md`, never edit `Shipped.md` (the only exception is release promotion, Part 4). Also update: the rule's `docs/rules/MoqXXXX.md` metadata table, both README tables if category/title changed, and `DiagnosticCategoryTests.cs`.

**Known discrepancy — defer to root policy, escalate the divergence (2026-07-02):** the live `AnalyzerReleases.Unshipped.md` currently contains a `### Changed Rules` section (14 recategorized rules with old/new columns). The root `.github/copilot-instructions.md:553` forbids this — it says `Unshipped.md` must **only** contain `### New Rules`, with a changed shipped rule recorded there as if it were a new rule. That root file is a higher-priority policy than this skill (see `moq-analyzers-auto`: copilot-instructions wins on conflict), so **do not default to the live layout.** When you record a new shipped-rule category/severity change, add it as a `### New Rules` row per copilot-instructions. The upstream Roslyn release-tracking format tolerates both shapes, which is why the pre-existing `### Changed Rules` block — accepted on `main` since PR #1087 — still passes CI; leave that block in place (never restructure it in an unrelated PR), but do not append to it. Because the on-disk file and the written policy genuinely diverge, this is a STOP-and-flag, not an auto-decision: state the divergence in the PR and let the maintainer decide whether to reconcile the file — never resolve it silently. `moq-analyzers-change-control` defers to this section, and `moq-analyzers-docs-and-writing` tracks the divergence as a stale-doc item.

Behavioral changes to a shipped rule (new detection cases, FP fixes) do not need an `AnalyzerReleases` entry at all — only ID/category/severity metadata is tracked there. They DO need issue-linked regression tests (see `moq-analyzers-validation-and-qa`) and a Moq-version-compatibility note in the PR.

## Part 4 — The release, end-to-end

Authoritative source: `CONTRIBUTING.md` "Release Process" (line 1007) — read it before your first release; this section condenses and verifies it.

### Versioning model (NBGV)

`version.json` at repo root (2026-07-02): `"version": "0.5.0-alpha.{height}"`. NBGV derives the version from this stem + branch + commit height. `publicReleaseRefSpec` allows public versions from `main`, `release/vX.Y.Z` branches, and `vX.Y.Z[-prerelease]` tags. On `main` you get prereleases like `0.5.0-alpha.123`; on a release branch `version.json` is edited to the bare stable version (`"0.5.0"`).

If any build fails with `MSB4018 ... GetBuildVersion task failed`: shallow clone; run `git fetch --unshallow`.

### Release steps (major/minor; patch differs as noted)

1. **Branch**: `git checkout -b release/v{X}.{Y}.0 main` (patch releases branch from the PRIOR release branch and cherry-pick fixes from `main`, oldest first).
2. **`version.json`**: set `"version": "{X}.{Y}.0"` (drop `-alpha`).
3. **Promote release notes**: move every row from `src/Analyzers/AnalyzerReleases.Unshipped.md` into `src/Analyzers/AnalyzerReleases.Shipped.md` under a new `## Release {X}.{Y}.0` heading. This is the ONLY sanctioned edit to `Shipped.md`. `New Rules` rows stay `New Rules`; per copilot-instructions, this promotion step is where `Changed Rules`/`Removed Rules` sections get written into `Shipped.md` for shipped-rule modifications. (Precedent: `## Release 0.4.0` in `Shipped.md` has both `### New Rules` and `### Changed Rules`.)
4. **Reset `Unshipped.md`** to the empty header (comment lines + `### New Rules` + table header only — exact text in `CONTRIBUTING.md:1071-1082`).
5. Commit `chore(release): prepare v{X}.{Y}.0 release branch`, push the branch, wait for CI green.
6. **Bump `main`**: on `main`, set `version.json` to the next dev stem (e.g., `"0.6.0-alpha.{height}"`). Precedent commit: `bae5141` "chore(release): bump version to 0.5.0-alpha for next development cycle". Nothing automates this — a forgotten bump makes `main` produce prerelease versions of an already-shipped number.
7. **Publish**: create a GitHub Release — Tag `v{X}.{Y}.{Z}`, Target `release/v{X}.{Y}.{Z}`, Title `v{X}.{Y}.{Z}`. Publishing the release fires `.github/workflows/release.yml`.

### What release.yml actually gates (read 2026-07-02)

`release.yml` triggers `on: release: types: [published]` (plus manual `workflow_dispatch`) and runs two jobs:

1. `build`: `uses: ./.github/workflows/main.yml` — the FULL CI pipeline (build with PedanticMode, 3,300+ tests, 9-way analyzer-load-test matrix, perf gate) reruns on the release commit. A red main.yml means no publish.
2. `publish` (needs `build`): downloads the `packages` artifact (uploaded by main.yml from `./artifacts/package`), then:
   - **Version-verify gate** — `if: github.event_name == 'release'`: strips `v` from the tag, parses the version out of each non-symbols `.nupkg` filename, and hard-fails on mismatch: `Write-Error "Package version '$nupkgVersion' does not match release tag '$tag'"`. This is what catches a tag created against the wrong branch or a stale `version.json`.
   - **Push** — `dotnet nuget push $file --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate`, retried up to 3 times with backoff.

Caution (audit finding D-7, open as of 2026-07-02): the version-verify step is skipped on `workflow_dispatch` runs — a manual dispatch pushes whatever version NBGV computed. Do not use `workflow_dispatch` on `release.yml` to publish; always publish via a GitHub Release event.

### Release checklist (from `CONTRIBUTING.md:1158`)

- [ ] Release branch created from correct base
- [ ] `version.json` updated to stable version
- [ ] `AnalyzerReleases.Shipped.md` updated, `Unshipped.md` cleared
- [ ] All bug fixes cherry-picked (patch releases)
- [ ] All tests pass
- [ ] Performance benchmarks show no regressions
- [ ] CI/CD is green on the release branch
- [ ] Release notes/description prepared
- [ ] GitHub Release created with correct tag and target branch

Plus (this skill's addition, from step 6 above): post-release, bump the `main` version stem.

## Part 5 — Retirement / deprecation: POLICY GAP

There is **no documented rule-retirement or deprecation policy** in this repo (2026-07-02): nothing in CONTRIBUTING.md, copilot-instructions, or the ADRs describes how to sunset a rule, and no rule has been deprecated in the modern (2024+) era. Do not invent a process; if asked to retire a rule, stop and get maintainer direction.

Known historical facts to reason from (not a policy):

- Exactly one removal exists in the record: `## Release 0.0.6` in `AnalyzerReleases.Shipped.md` has a `### Removed Rules` section removing the original Moq1003 (2017-era, pre-current-maintainer).
- That ID was later reused for `InternalTypeMustHaveInternalsVisibleToAnalyzer` (currently in `Unshipped.md`). ID reuse is generally considered unsafe (consumers' suppressions/`.editorconfig` entries keyed to the old meaning silently apply to the new rule).
- The release-tracking format has first-class `### Removed Rules` support, so the mechanical path exists; what is missing is the policy (deprecation window? Info-severity sunset release? doc tombstone?). Treat any proposal here as an open candidate requiring maintainer sign-off.

## Worked example — what Moq1600 actually shipped (commit 3f98710)

Condensed from `git show --stat 3f98710`:

| File | Δ | Checklist row |
|------|---|---------------|
| `src/Common/DiagnosticIds.cs` | +1 | 1 — `ProtectedSetupUsesItMatcherInsteadOfItExpr = "Moq1600"` |
| `src/Analyzers/ProtectedSetupShouldUseItExprAnalyzer.cs` | +241 | 2 — analyzer |
| `src/Analyzers/AnalyzerReleases.Unshipped.md` | +1 | 3 — `Moq1600 \| Usage \| Warning \| ProtectedSetupShouldUseItExprAnalyzer` |
| `docs/rules/Moq1600.md` | +95 | 4 — rule doc, all template sections |
| `docs/rules/README.md` | +4 | 5 — master-table row + range-table row for 1600–1699 |
| `README.md` | +1 | 6 — root table row (now line 42) |
| `tests/.../ProtectedSetupShouldUseItExprAnalyzerTests.cs` | +237 | 7 — Theory/MemberData, old+new Moq splits, no-Moq fact, inline doppelganger |
| `tests/.../DiagnosticCategoryTests.cs` | +1 | 8 — Usage row |
| `tests/Moq.Analyzers.Benchmarks/Moq1600Benchmarks.cs` | +91 | 9 — `[Params(1, 1_000)]`, baseline + with-diagnostics |
| `src/Common/WellKnown/MoqKnownSymbols.cs` | +42 | 10 — `IProtectedMock1` (+ its 5 method groups), `ItExpr`, `ProtectedExtension` |

Total: 10 files, +713/−1. No code fix (doc says `CodeFix | False`), so no `src/CodeFixes` file. The one known omission — no `MoqKnownSymbols` resolution tests for the new symbols — is documented in Part 1 §10; do not replicate it.

## When NOT to use this skill

| If you need… | Use instead |
|---------------|-------------|
| How to WRITE the analyzer body (operations, symbols, spans, Roslyn APIs) | `roslyn-analyzer-reference` |
| The repo's architectural invariants (ADR-001 symbol detection, banned APIs, concurrency rules) | `moq-analyzers-architecture-contract` |
| Branch naming, PR evidence requirements, merge gates, bot feedback rules | `moq-analyzers-change-control` |
| Test-quality bar, coverage requirements, snapshot/Verify mechanics | `moq-analyzers-validation-and-qa` |
| Build/SDK setup, PedanticMode mechanics, local environment | `moq-analyzers-build-and-env` |
| Perf gate internals (PerfDiff, baselines) and diagnostic tooling | `moq-analyzers-diagnostics-and-tooling` |
| Moq API semantics your rule must model | `moq-api-reference` |
| Why past FP battles went the way they did (Moq1203 saga etc.) | `moq-analyzers-failure-archaeology` |
| Debugging a broken analyzer or failing span test | `moq-analyzers-debugging-playbook` |
| Rule doc prose style | `moq-analyzers-docs-and-writing` |
| .editorconfig/options/flags a rule can expose | `moq-analyzers-config-and-flags` |
| BCL/API-design quality bar for public surface | `dotnet-api-design-standards` |

## Provenance and maintenance

- Rule count / ID list: `grep -c 'internal const string' src/Common/DiagnosticIds.cs` (25 IDs, Moq1209 reserved, 2026-07-02).
- helpLinkUri pattern still universal: `grep -c GitCommitId src/Analyzers/*.cs | grep -v ':0'` (25 uses, 2026-07-02).
- Severity distribution: `grep -h 'DiagnosticSeverity\.' src/Analyzers/*.cs | grep -o 'DiagnosticSeverity\.[A-Za-z]*' | sort | uniq -c` (5 Error / 18 Warning / 2 Info descriptors, 2026-07-02; note one Error hit is an internal check in LinqToMocksExpressionShouldBeValidAnalyzer.cs:215, not a descriptor).
- Unshipped/Shipped shape (and the Changed-Rules discrepancy vs copilot-instructions.md:553): `sed -n '1,30p' src/Analyzers/AnalyzerReleases.Unshipped.md`.
- Release-tracking analyzer pin: `grep CodeAnalysis.Analyzers build/targets/codeanalysis/Packages.props` (5.3.0, 2026-07-02).
- Version stem: `grep '"version"' version.json` (`0.5.0-alpha.{height}`, 2026-07-02); post-release bump precedent `git show bae5141 --stat`.
- release.yml gates: `sed -n '1,80p' .github/workflows/release.yml` (verify step at `if: github.event_name == 'release'`; push uses `--skip-duplicate` with 3 retries).
- Release process prose: `grep -n 'Release Process' CONTRIBUTING.md` (line 1007; checklist at 1158).
- Moq1600 worked example: `git show --stat 3f98710`.
- Benchmark file convention/coverage: `ls tests/Moq.Analyzers.Benchmarks/Moq1*Benchmarks.cs | wc -l` (18 files, 2026-07-02).
- AllAnalyzersVerifier namespace predicate: `grep -n '"Moq.Analyzers"' tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs`.
- Retirement policy still absent: `grep -rin 'deprecat' CONTRIBUTING.md .github/copilot-instructions.md docs/` (no rule-deprecation policy hits, 2026-07-02).
- Last verified: 2026-07-02 against commit 05135b2.
