---
name: moq-analyzers-change-control
description: "Apply this repo's change-control rules before making, gating, or reviewing ANY change to moq.analyzers — new rules, rule behavior changes, code fixes, shared helpers, dependencies, CI workflows, docs, or releases. Load it when asking \"what gates must this PR pass\", \"can I edit AnalyzerReleases.Shipped.md\", \"who may bump Roslyn\", \"what evidence goes in the PR body\", \"is an ADR needed\", \"when is editing AnalyzerReleases.Shipped.md legal\" (full promotion runbook: moq-analyzers-rule-lifecycle), or when a CI check (semantic-pr-check, perf, analyzer-load-test, todo-scanner, RS2000) blocks you. Do NOT load it for build/test command troubleshooting (moq-analyzers-build-and-env), step-by-step rule authoring (moq-analyzers-rule-lifecycle), Roslyn API questions (roslyn-analyzer-reference), or diagnosing why an analyzer misbehaves (moq-analyzers-debugging-playbook)."
---

# Change control in moq.analyzers

## Why this repo is strict (read this first)

Moq.Analyzers is a Roslyn analyzer package: a plugin DLL that NuGet injects into
the **customer's compiler** (Visual Studio, `dotnet build`, MSBuild). One bad
release does not break one server — it breaks **every build of every consumer**
until they pin an older version. This has actually happened: release v0.4.0
shipped a dependency that could not load in .NET 8 SDK hosts (compiler warning
CS8032, "analyzer could not be loaded"), and v0.4.1 existed solely to undo it
(issue #850, fix commit `38943ac`). The maintainer's standing order: hold all
code to .NET Base Class Library (BCL) quality standards; everything must be
air-tight and thoroughly tested.

Priority order (from `.github/copilot-instructions.md`, "Mission-Critical
Quality Standard"): (1) no analyzer crashes, (2) no false positives/negatives,
(3) per-keystroke performance, (4) thread safety. Every gate below exists
because one of these was violated once.

Term definitions used throughout:

| Term | Meaning |
|---|---|
| FP / FN | False positive (diagnostic fires on correct code) / false negative (diagnostic misses bad code) |
| ADR | Architecture Decision Record, `docs/architecture/ADR-0NN-*.md`; 10 exist (ADR-001..ADR-010) |
| PedanticMode | MSBuild property that turns all warnings into errors (`build/targets/codeanalysis/CodeAnalysis.targets:3-5`); defaults ON in CI, OFF locally — warnings pass locally but fail CI |
| RS2000-family | Diagnostics from Microsoft.CodeAnalysis.Analyzers that force every rule to be declared in `AnalyzerReleases.*.md`; warning locally, error under PedanticMode |
| AD0001 | Roslyn's "analyzer threw an exception" diagnostic; the analyzer is disabled for the session — the worst possible outcome |
| CS8032 | "Analyzer could not be loaded" — the host runtime cannot load a bundled dependency version |
| Squash merge | The only merge strategy enabled; the PR title becomes the commit subject |

## Change classification table

Classify your change FIRST; the class determines the gates. A PR spanning two
classes must pass the union of both gate sets.

| Change class | Touches | Gates beyond the universal set (below) |
|---|---|---|
| New rule | `src/Common/DiagnosticIds.cs`, new `src/Analyzers/*Analyzer.cs`, `AnalyzerReleases.Unshipped.md`, `docs/rules/Moq{Id}.md`, `docs/rules/README.md`, tests, `tests/Moq.Analyzers.Benchmarks/Moq{Id}Benchmarks.cs` | RS2000-family fails the CI build if the Unshipped row is missing; rule-ID must fit the range table in CONTRIBUTING.md ("Rule ID Range Allocation"); `perf` job (required status check) compares benchmarks against baseline; full checklist in **moq-analyzers-rule-lifecycle** |
| Rule behavior change (severity, category, span, logic) | Existing analyzer + its tests | An `AnalyzerReleases.Unshipped.md` entry is required ONLY when the change is to ID/category/severity **metadata** (never touch Shipped). A pure **logic/span/FP** fix needs NO AnalyzerReleases row — only issue-linked regression tests pinning old-behavior cases (precedent: FP fixes `5eec7e1`, `4b705e2` touched no release files). Always: span discipline (non-negotiables table) + a Moq-version compatibility note in the PR body. Full rule: **moq-analyzers-rule-lifecycle Part 3**. |
| Code fix (new or changed) | `src/CodeFixes/*Fixer.cs` | Mandatory test pattern: `[Theory]` + `[MemberData]` + `public static IEnumerable<object[]>` (CONTRIBUTING.md §"Data-Driven Testing for Code Fixes (MANDATORY PATTERN)"); fixed output must be valid compiling C# |
| Shared-helper change | `src/Common/**` (used by up to 25 analyzers) | Full test suite is the minimum bar — a Common change can silently alter every rule; run the perf pipeline if the helper is on a hot path; expect reviewer demand for adversarial cases (see non-negotiable 8) |
| Dependency change | `Directory.Packages.props`, `build/targets/*/Packages.props`, `renovate.json`, `THIRD-PARTY-NOTICES.TXT` | Dependency-change rules section below; host-compat triple enforcement; `dotnet snitch --strict` (CI build job) fails on unused packages |
| CI / workflow change | `.github/workflows/**`, `.github/actions/**` | `actionlint` + `gh act -n` dry-run output pasted into PR body (CONTRIBUTING.md §"Workflow File Validation"); keep third-party actions SHA-pinned with `# vX.Y.Z` comment (repo convention, one known stray: `tech-debt-tracker.yml` uses a tag pin) |
| Docs only | `docs/**`, `*.md` outside src | `check-paths` job in `main.yml:40-82` skips build/test/perf for docs-only PRs; markdownlint (pre-commit hook + super-linter in `linters.yml`); note `AnalyzerReleases.*.md` and `*.verified.*` are excluded from super-linter — do not "fix" their formatting |
| Release promotion | `version.json`, `AnalyzerReleases.{Shipped,Unshipped}.md`, release branch | The ONLY legal time to edit Shipped; summary below, full runbook in **moq-analyzers-rule-lifecycle Part 4** |

**Universal gate set (every class, every PR):**

```bash
dotnet format                                                    # then commit the result
dotnet build /p:PedanticMode=true                                # CI-parity; plain `dotnet build` is lenient
dotnet test --settings ./build/targets/tests/test.runsettings    # all tests pass; produces Cobertura coverage
```

Plus: Codacy CLI analysis on changed files, no `*.received.*` files committed
(Verify snapshot failure artifacts), all bot feedback addressed or explicitly
rebutted in the PR description. CONTRIBUTING.md §"Strict Workflow Requirements"
states PRs failing format/build/test/Codacy **will be closed without review**.

Git hooks enforce a subset automatically (Husky.NET, `.husky/task-runner.json`):
pre-commit = large-file check, `dotnet format --verify-no-changes` on staged
.cs, markdownlint-cli2, yamllint, actionlint, JSON validation, shellcheck;
pre-push = TODO scanner (`-FailOnUnlinked`) + CI-parity Release build + full
test run (`build/scripts/hooks/Invoke-PrePushBuild.ps1`). `--no-verify` is
permitted only for WIP commits per CONTRIBUTING.md — never for the commit you
intend to push for review; the same checks run in CI and will fail there
instead (hook mechanics: moq-analyzers-build-and-env §4).

## The non-negotiables

Each rule below was paid for with a real incident. Commits verifiable via
`git log --oneline <sha>`; issue numbers are GitHub issues in
rjmurillo/moq.analyzers. (Incident details verified 2026-07-02.)

| # | Rule | Rationale | Incident evidence |
|---|---|---|---|
| 1 | **Symbol-based detection only** (ADR-001). Never decide semantics from a string name; `ISymbol` + `SymbolEqualityComparer` + `MoqKnownSymbols` always. A name check is allowed ONLY as a cheap pre-filter before an authoritative symbol check. Removing an existing string fallback requires proof the symbol path covers every case. | String matching breaks on aliases, renames, and user types named like Moq types (`DoppelgangerTestHelper` tests pin this); a wrong fallback silently changes rule behavior across all consumers. | Migration campaign #245→#1030 (final commit `a974999`). A fallback-removal attempt FAILED and was documented instead (`5172cf3`, #768); it succeeded only after full symbol coverage incl. ``IRaise`1`` was added (`35d363d`, #770). `src/BannedSymbols.txt` bans `Compilation.GetTypeByMetadataName` (use KnownSymbols) and raw `Diagnostic.Create` (use `DiagnosticExtensions.CreateDiagnostic`). |
| 2 | **`AnalyzerReleases.Shipped.md` is immutable** outside release promotion. New rules and any ID/category/severity **metadata** change to an already-shipped rule go in `Unshipped.md`; a pure behavior/logic/FP fix changes none of the tracked fields, so it has NO AnalyzerReleases entry (see the change-class table above and moq-analyzers-rule-lifecycle Part 3). | Shipped is the historical record consumed by Roslyn's release-tracking analyzer; rewriting it lies about what past package versions contained. | `.github/copilot-instructions.md:552`: "Do not modify AnalyzerReleases.Shipped.md. This file is an immutable record of past releases." Prior mix-up: swapped file names, issue #983. |
| 3 | **Dependency ceilings for shipped DLLs**: System.Collections.Immutable ≤ 8.0.0, System.Reflection.Metadata ≤ 8.0.0, AnalyzerUtilities < 4.14.0, Roslyn pinned 4.8 (ADR-003/ADR-004). | Bundled assemblies must load inside a .NET 8 SDK / VS 2022 17.8 host. Exceeding host versions = CS8032, analyzers silently dead for every consumer on that SDK. | Incident #850: v0.4.0 transitively pushed SCI to 10.0.0.0 → CS8032 for all .NET 8 SDK users; fixed `38943ac` (#888); release v0.4.1 existed for this. Now triple-enforced: `ValidateAnalyzerHostCompatibility` MSBuild target (`build/targets/packaging/Packaging.targets`), inline DLL-reference check in `main.yml` build job (~line 104), and the 9-way `analyzer-load-test` CI matrix (net8/9/10 CLI + net472/48/481 MSBuild). |
| 4 | **Never raise S1135 (TODO tracking) above `suggestion`.** TODO discipline is enforced by the todo-scanner instead: every `TODO`/`FIXME`/`HACK`/`UNDONE` must be issue-linked as `TODO(#123)`. | Under PedanticMode all warnings are errors; making TODO a warning turns every tracked TODO into a CI build failure — the repo locked itself out of CI. | Commit `3d4f7ff` (2026-03-06) raised it; reverted the next day by `b1439ab`; the build error was the codebase's own issue-linked `TODO(#1012)` comment (#1012 itself tracks an unrelated callback-validation enhancement, not this incident) — per moq-analyzers-failure-archaeology §4. Current setting: `.editorconfig:420` `dotnet_diagnostic.S1135.severity = suggestion`. Scanner: `build/scripts/todo-scanner/Scan-TodoComments.ps1`, run in pre-push and `tech-debt-tracker.yml`. |
| 5 | **PowerShell files are LF** (`*.ps1`, `*.psm1`, `*.psd1` have `text eol=lf` in `.gitattributes`), per ADR-010. Never override with CRLF. | CRLF makes `#>` block-comment terminators end in `\r`, which PowerShell cannot parse when hooks run from Git Bash / Unix shells — every push was blocked. | Issue #1081 (pre-push hook parse error in `Scan-TodoComments.ps1`); ADR-010 documents root cause and rejects the CRLF alternative explicitly. |
| 6 | **Diagnostic spans are character-precise and pinned by tests.** Test markup `{\|Moq1002:...\|}` asserts ID + exact span. A span test failure means your syntax-tree navigation is wrong — STOP after the first failure, re-derive the logic; escalate to a human after the second. Never "fix" the test to match your output. | The span is the user-visible squiggle; an off-by-one span on millions of consumer builds is a shipped bug. Historically, models that adjusted spans to make tests pass produced plausible-but-wrong analyzers. | `.github/copilot-instructions.md:511-513`: spans MUST be character-precise; a span test failure is a CRITICAL FAILURE; stop-and-escalate protocol verbatim there. |
| 7 | **Every FP/FN fix ships with issue-linked regression tests in the same PR.** | FP fixing historically converged only when each fix pinned its trigger; the Moq1203 saga took FIVE separate patches because each fix covered one syntactic wrapper (chaining `6ec810c` #886 → parentheses `c270302` #895 → sibling rules `894313b` #907 → delegate overloads `0bef80b` #919 → extension methods `5eec7e1` #1086). Syntactic wrappers (parens, extension methods, fluent chains) are a mandatory test axis. | The five commits above, plus Moq1302: fix `4b705e2` (#1017) was followed by a dedicated regression-suite commit `3399297` (#1020) referencing the originating report #1010. This is settled practice evidenced by history (not a literal CONTRIBUTING sentence — see UNVERIFIED note in Provenance). |
| 8 | **AI-written code gets human-added adversarial cases before merge.** An AI that writes both implementation and tests shares blind spots between them; a human must add boundary cases the tests did not think of: literals, captured locals, static/const members, method calls, external constants. | AI-authored Moq1302 (PR #511, Copilot-authored commit `458ca5d`, 2025-06-25) shipped with comprehensive-looking tests, then produced live FPs on canonical LINQ-to-Mocks patterns (#1010). The human-authored follow-up suite `3399297` added exactly these categories: static lambda + external constant, captured local variable, `!=` with external constant, chained properties. | Commits `458ca5d`, `4b705e2`, `3399297`; the maintainer names "plausible-but-wrong AI code" as the costliest historical failure mode. |
| 9 | **Span/allocation discipline on hot paths.** Analyzers run per keystroke; no LINQ chains, `ToArray()`, or string formatting before the code knows a diagnostic will be reported. `EnableConcurrentExecution` + `ConfigureGeneratedCodeAnalysis` + `IsMockReferenced()` early exit in every `Initialize`; `MoqKnownSymbols` constructed once per `CompilationStartAnalysisContext` (ADR-006). | Per-operation allocation caused measurable IDE lag; the perf gate (`perf` job, ADR-008) is a required status check precisely to stop this class of regression. | Allocation-fix campaign: `9febdda` (#1026, MoqKnownSymbols per compilation not per operation), `3b5ac71` (#1033, same hoist across 9 analyzers), `7595080` (#1050, array allocations in constructor matching). |
| 10 | **No trial-and-error; STOP when unsure.** If you cannot explain why your approach is correct, halt and ask — do not iterate until green. | Iterating-to-green produces code that passes existing tests while being semantically wrong (the exact failure mode behind rules 6–8). | `.github/copilot-instructions.md` "Escalation and Stop Conditions" (line ~72) and Quick Reference table: "Never guess or use trial-and-error; STOP if unsure." |

## AnalyzerReleases: Unshipped → Shipped promotion (release time only)

Background: Roslyn's release-tracking analyzer (from the
Microsoft.CodeAnalysis.Analyzers package, pinned 5.6.0 in
`build/targets/codeanalysis/Packages.props`) requires
`src/Analyzers/AnalyzerReleases.Unshipped.md` and
`AnalyzerReleases.Shipped.md`. A new rule without an Unshipped row raises an
RS2000-family diagnostic → build failure under PedanticMode (CI and pre-push).

Day-to-day (any PR): add/modify rows in **Unshipped only** — never touch
Shipped (non-negotiable 2). Which SECTION of Unshipped a shipped-rule change
goes in is a known instruction/file divergence:
`.github/copilot-instructions.md:553` says Unshipped must only contain a
`### New Rules` section, but the live file has carried a `### Changed Rules`
section since PR #1087, and both shapes build. The canonical treatment and
default live in **moq-analyzers-rule-lifecycle Part 3**; if your change forces
you to choose a shape, surface the choice explicitly in the PR — never
silently override the written rule — and do not restructure the file in an
unrelated PR.

At release time, on a `release/v{X}.{Y}.{Z}` branch: move the Unshipped rows
into `AnalyzerReleases.Shipped.md` under a new `## Release {X}.{Y}.{Z}` heading,
reset Unshipped to its empty header, and commit together with the
`version.json` stable-version bump
(`chore(release): prepare v{X}.{Y}.{Z} release branch`). Use the FULL target
version — `{Z}` is `0` for a major/minor release but the patch number for a
patch (e.g. `0.4.1`); never hard-code `.0`. That is the ONLY legal
edit to Shipped. The verbatim CONTRIBUTING steps (§"Creating a Major or Minor
Release" 3–4) and the full release runbook — patch-branch strategy,
post-release `main` version-stem bump, what `release.yml` gates before
pushing to nuget.org — live in **moq-analyzers-rule-lifecycle Part 4**.

## PR evidence requirements (what goes in the PR body)

CONTRIBUTING.md §"Strict Workflow Requirements" + §"Validation Evidence
Requirements" + `.github/copilot-instructions.md` §"Required Validation
Evidence for PRs". Missing evidence = not reviewed.

Checklist — paste console output (or screenshots/CI links) for each:

- [ ] `dotnet format` output (and its changes committed)
- [ ] `dotnet build /p:PedanticMode=true` output — zero warnings
- [ ] `dotnet test --settings ./build/targets/tests/test.runsettings` output — all pass
- [ ] Code coverage summary (Cobertura output lands under `artifacts/TestResults/coverage/`); coverage validation is MANDATORY before completing any task; must not reduce critical-path coverage without justification
- [ ] Codacy CLI analysis output on every changed file — REQUIRED for every PR, including a clean run (`.github/copilot-instructions.md` lists Codacy analysis output among the mandatory PR evidence and requires running it after every file edit; paste the output even when nothing was found)
- [ ] **Moq version compatibility note** for any analyzer/test change: which Moq versions are targeted (test matrix runs 4.8.2 and 4.18.4) and how test data is grouped
- [ ] Workflow changes only: `actionlint` output + `gh act -n` dry-run summary showing every job `Job succeeded`:

```bash
actionlint
gh act -n -W .github/workflows/main.yml \
  -P ubuntu-24.04-arm=catthehacker/ubuntu:act-latest \
  -P windows-latest=catthehacker/ubuntu:act-latest \
  -P windows-2022=catthehacker/ubuntu:act-latest \
  -P windows-2025-vs2026=catthehacker/ubuntu:act-latest
```

- [ ] Performance impact assessed for new analyzers / hot-path changes (benchmark results; `perf` is a required status check)
- [ ] All bot feedback (Codacy, linters, review bots) addressed, or a rebuttal written in the PR description
- [ ] No `*.received.*` files in the diff

## Commit, branch, and merge conventions

| Convention | Rule | Enforcement |
|---|---|---|
| Commit format | Conventional Commits: `type(scope): description`, lowercase type, imperative mood. Types: feat, fix, docs, style, refactor, test, chore, ci, perf, build | `semantic-pr-check.yml` validates the **PR title** (amannn/action-semantic-pull-request); since squash merge makes the title the commit subject, a bad title becomes a bad commit |
| Branch names | `feature/issue-{n}`, `fix/issue-{n}`, `docs/issue-{n}`, `ci/issue-{n}`, `chore/issue-{n}` | CONTRIBUTING.md §"Branch Naming Convention" (review convention) |
| Merge strategy | Squash merge ONLY; merge commits and rebase merges are disabled | Repo settings; stated in CONTRIBUTING.md §"Strict Workflow Requirements" |
| Release branches | `release/v{X}.{Y}.{Z}`; major/minor branch from `main`, patches branch from the prior release branch and cherry-pick fixes oldest-first | CONTRIBUTING.md §"Branch Strategy" |
| Label check | `release-drafter` label check may show failed — it is NOT a required check and does not block merge | CONTRIBUTING.md §"Strict Workflow Requirements" |

Worked example of a good PR title / squash commit from this repo's history:

```text
fix: suppress Moq1203 for setup wrapped in extension methods (#1086)
perf: hoist MoqKnownSymbols to per-compilation in 9 analyzers (#1033)
chore(release): prepare v0.4.1 release branch
```

## Dependency-change rules

Authoritative docs: `docs/dependency-management.md` + `renovate.json` +
CONTRIBUTING.md §"Dependency Management". Renovate is the sole update bot
(Dependabot config removed; GitHub security-alert PRs may still appear and are
handled by `dependabot-approve-and-auto-merge.yml`).

Category policies (all verified against `renovate.json`, 2026-07-02):

| Category | Packages (examples) | Policy |
|---|---|---|
| Shipped in the nupkg — EXTREME CAUTION | Microsoft.CodeAnalysis.CSharp(+Workspaces) 4.8, AnalyzerUtilities 3.3.4, System.Collections.Immutable 8.0.0, System.Reflection.Metadata, System.Formats.Asn1 | Roslyn packages are in Renovate `ignoreDeps` — **the bot never proposes them; a human may change the Roslyn pin only by superseding ADR-003 (and AnalyzerUtilities per ADR-004); there is no routine path to bump it.** SCI/SRM capped `<=8.0.0`, AnalyzerUtilities `<4.14.0`, all with `automerge: false` + `analyzer-compat` label → manual review mandatory |
| Build-time analyzers — safe | StyleCop, SonarAnalyzer, Meziantou, Roslynator (`build/targets/codeanalysis/Packages.props`) | Automerge minor/patch; review majors (new rules can break the warnings-as-errors build) |
| Test framework — safe | xunit, Verify.Xunit, Microsoft.NET.Test.Sdk (`build/targets/tests/Packages.props`) | Automerge minor/patch |
| Benchmark tooling — coordinated | BenchmarkDotNet (`automerge: false`, `benchmark-tooling` label); Perfolizer **disabled** in Renovate (BDN requires exactly `[0.6.1]`) | Update Perfolizer only when BenchmarkDotNet bumps its constraint; otherwise NU1608 + runtime risk |
| PerfDiff tool — frozen | System.CommandLine(+Rendering) **disabled** in Renovate | Requires PerfDiff code changes to update (issue #914) |

Additional hard rules for any dependency PR:

1. **Bundled dependency ⇒ `THIRD-PARTY-NOTICES.TXT` update in the same PR.**
   "Bundled" = its compiled assembly is packed into the nupkg (`Pack="true"`
   items in `src/Analyzers/Moq.Analyzers.csproj`; packed assemblies as of
   2026-07-02: Moq.Analyzers.dll, Moq.CodeFixes.dll,
   Microsoft.CodeAnalysis.AnalyzerUtilities.dll).
   Compile-time-only Roslyn references and dev tools do not need attribution.
   Full research procedure: CONTRIBUTING.md §"Third-Party License Attribution".
2. **Host-compat is checked three ways** — do not weaken any of them:
   `ValidateAnalyzerHostCompatibility` target, the inline PowerShell DLL-reference
   check in the `main.yml` build job (max SCI/SRM = 8.0.0.0), and the
   `analyzer-load-test` matrix. `AnalyzerAssemblyCompatibilityTests` pins the
   same bound at test level.
3. **VersionOverride pattern**: non-shipped projects (benchmarks, PerfDiff) may
   float above the central pin via `VersionOverride` in their `.csproj`; the
   central pin exists to protect the shipped DLLs. Manage overrides manually —
   Renovate caps do not see them.
4. **`dotnet snitch --strict`** in the CI build job fails on unused package
   references — remove a dependency's `PackageReference` when you remove its
   last use.
5. Auto-merge reality (know it when reviewing): non-major Renovate updates
   without a cap rule are auto-approved by
   `dependabot-approve-and-auto-merge.yml` (PAT `GH_ACTIONS_PR_WRITE`) and
   auto-merged by Renovate (`platformAutomerge: true`) with only CI as the
   gate. The 2026-07-02 security audit flagged this as the repo's material
   policy risk (unattended supply chain into a compiler plugin) — treat any
   proposal to WIDEN automerge scope as a change requiring maintainer sign-off,
   and never route a code change through a bot-labeled PR to inherit its
   auto-approval.

## When an ADR is required

An ADR is the unit of change for architecture. Ten accepted ADRs exist
(2026-07-02) in `docs/architecture/`:

| ADR | Locks in |
|---|---|
| 001 | Symbol-based detection over string matching |
| 002 | netstandard2.0 target for shipped assemblies |
| 003 | Roslyn (Microsoft.CodeAnalysis) pinned to 4.8 |
| 004 | AnalyzerUtilities capped at 3.3.4 |
| 005 | Central Package Management with transitive pinning |
| 006 | MoqKnownSymbols/WellKnown-types pattern, once per compilation |
| 007 | Prefer RegisterOperationAction over RegisterSyntaxNodeAction |
| 008 | BenchmarkDotNet + PerfDiff as the perf regression gate |
| 009 | xunit + Roslyn test infrastructure |
| 010 | eol=lf for PowerShell files |

Rules:

- **Contradicting an accepted ADR requires a new ADR that supersedes it**
  (the front matter has `supersedes`/`superseded_by` fields for exactly this).
  Example: raising the Roslyn pin from 4.8 means superseding ADR-003 — ADR-003
  itself documents the required steps (update `Directory.Packages.props`, full
  test suite, benchmark verification, docs) and the user-impact assessment
  (excludes consumers on VS < 17.8).
- **A new decision of the same weight** (a version pin, a mandatory pattern
  across all analyzers, a CI gate, a target-framework choice) should get its
  own ADR before implementation. (Convention inferred from the existing corpus
  — every such decision in this repo has one; there is no written meta-policy
  file. Labeled as convention, 2026-07-02.)
- ADRs record decisions; they are not a bypass. Writing an ADR does not exempt
  a PR from any gate in this document.

## Worked example: gating a rule-behavior-change PR end to end

Scenario: you fix the Moq1202 FP on non-generic `EventHandler` events (audit
finding, filed as an implementation issue in the #1241–#1278 range).

1. Classify: **rule behavior change** + possibly **shared-helper change**
   (the defect lives in `src/Common/EventSyntaxExtensions.cs`, which also
   feeds Moq1204) → union of both gate sets; run the FULL test suite, not
   just the two rules' tests.
2. Branch `fix/issue-{n}`; PR title `fix: accept single-argument Raise for
   non-generic EventHandler events` (semantic-pr-check will validate it).
3. Write the regression tests FIRST, markup-pinned spans, covering the
   canonical pattern `mock.Raise(m => m.Closed += null, EventArgs.Empty)` for
   BOTH Moq1202 and Moq1204, fanned across both Moq versions
   (`WithOld/New/BothMoqReferenceAssemblyGroups`). Add the adversarial axis
   from non-negotiables 7–8: custom `(object, XEventArgs)` delegates,
   two-argument form, wrappers.
4. No new rule and no severity/category change ⇒ no `AnalyzerReleases` edit
   needed; touching Shipped is forbidden regardless.
5. Local gates: `dotnet format`; `dotnet build /p:PedanticMode=true`;
   `dotnet test --settings ./build/targets/tests/test.runsettings`; Codacy.
6. PR body: all evidence blocks + Moq compatibility note ("verified against
   Moq 4.8.2 and 4.18.4; Raise single-arg form is legal in both") + link to
   the originating issue; the tests reference the issue number in
   names/comments per the FP-fix convention.
7. Expect the `perf` required check to run (Common change = code path filter
   hit); a span test failure at any point = STOP per non-negotiable 6.

## When NOT to use this skill

| Your task | Use instead |
|---|---|
| Set up SDK/PATH, fix build errors, run tests/coverage locally | moq-analyzers-build-and-env |
| Author a new rule step by step (IDs, descriptors, docs template, benchmarks) | moq-analyzers-rule-lifecycle |
| Understand Roslyn APIs (ISymbol, IOperation, registration model) | roslyn-analyzer-reference |
| Understand Moq's API surface and version differences | moq-api-reference |
| Diagnose a misbehaving analyzer / AD0001 / wrong span | moq-analyzers-debugging-playbook |
| Deep history of a past incident beyond the one-line evidence here | moq-analyzers-failure-archaeology |
| Architecture invariants and ADR contents in depth | moq-analyzers-architecture-contract |
| Test-quality standards, coverage strategy, Verify snapshots | moq-analyzers-validation-and-qa |
| .editorconfig severities, analyzer flags, suppression mechanics | moq-analyzers-config-and-flags |
| BCL/API-design standards the code is held to | dotnet-api-design-standards |
| Writing docs/rules pages and markdown standards | moq-analyzers-docs-and-writing |
| The FP-elimination campaign backlog (#1241–#1278) | moq-analyzers-fp-convergence-campaign |
| PerfDiff internals, binlogs, SARIF, tooling | moq-analyzers-diagnostics-and-tooling |

This skill also does not describe any way to bypass gates — there isn't one;
CONTRIBUTING.md's word is "closed", not "flagged".

## Provenance and maintenance

Re-verify before trusting volatile facts (all verified 2026-07-02):

- Gates exist in CI: `grep -nE "^  [a-z-]+:" .github/workflows/main.yml` (jobs: check-paths, build, test, analyzer-load-test, perf)
- Host-compat inline check + snitch: `grep -n "snitch --strict\|Validate analyzer host" .github/workflows/main.yml`
- PedanticMode wiring: `grep -n PedanticMode build/targets/codeanalysis/CodeAnalysis.targets build/scripts/hooks/Invoke-PrePushBuild.ps1`
- S1135 still `suggestion`: `grep -n "S1135" .editorconfig`
- Hook task list: `cat .husky/task-runner.json`
- Renovate pins/caps/ignores: `cat renovate.json` (ignoreDeps = 4 Microsoft.CodeAnalysis.* packages; caps on SCI/SRM/AnalyzerUtilities; Perfolizer + System.CommandLine disabled)
- Roslyn pin still 4.8 / AnalyzerUtilities 3.3.4: `grep -n "Microsoft.CodeAnalysis.CSharp\"\|AnalyzerUtilities" Directory.Packages.props`
- Shipped-file immutability rule text: `grep -n "AnalyzerReleases.Shipped" .github/copilot-instructions.md`
- Unshipped current layout (New Rules + Changed Rules divergence note): `cat src/Analyzers/AnalyzerReleases.Unshipped.md`
- Promotion procedure unchanged: CONTRIBUTING.md §"Creating a Major or Minor Release" steps 3–4
- PR evidence list unchanged: CONTRIBUTING.md §"Strict Workflow Requirements" and §"Validation Evidence Requirements"
- Semantic PR title gate: `cat .github/workflows/semantic-pr-check.yml`
- ADR count/status: `ls docs/architecture/` (10 files, all "Accepted" as of 2026-07-02)
- Incident commits still resolve: `git log --oneline -1 38943ac 3d4f7ff b1439ab 5172cf3 35d363d a974999 458ca5d 3399297 5eec7e1` (these span the full project history; in a shallow clone — CI checkouts and some fresh clones — old hashes report "not found" / "bad object", so run `git fetch --unshallow` first. Every hash is reachable in a full clone, verified 2026-07-04.)
- Issue states (may have closed/moved): #850, #1081, #1010, #986, #914, audit-filing range #1241–#1278 — `gh issue view <n>`. (#1012 is deliberately absent: it is the unrelated enhancement the S1135-tripping `TODO(#1012)` comment linked to, not an incident tracker)
- version.json stem (currently `0.5.0-alpha.{height}`): `cat version.json`
- UNVERIFIED as written policy (verified only as consistent historical practice): non-negotiable 7's exact phrasing "every FP/FN fix ships an issue-linked regression test in the same PR" — no single CONTRIBUTING sentence states it; evidence is the commit trail cited in the table.

Last verified: 2026-07-02 against commit 05135b2.
