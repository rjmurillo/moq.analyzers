---
name: moq-analyzers-research-frontier
description: "Navigate the five maintainer-confirmed open problems where moq.analyzers can advance the state of the art — corpus-based zero-false-positive validation, mutation-tested rule quality (#904), sequence-pattern analyzers (reviving #614-#617), full Moq semantic modeling (protected members, default interface members, MockBehavior data flow), and performance-as-a-tested-contract (#1265-#1269). Load this when asked \"what should we work on next at the research level\", when scoping a new ambitious capability, when someone proposes corpus testing, Stryker/mutation testing, MockSequence analysis, or per-keystroke perf budgets, or when deciding whether a frontier claim is publishable. Do NOT load it for fixing a known bug (moq-analyzers-fp-convergence-campaign), shipping a scoped new rule (moq-analyzers-rule-lifecycle), how to run experiments rigorously (moq-analyzers-research-methodology), or proof techniques for a single claim (moq-analyzers-proof-toolkit)."
---

# Research frontier: open problems where this project can lead

**Status of everything in this file: OPEN / CANDIDATE (2026-07-02).** None of
these problems is solved. Nothing here may be claimed in README, docs, release
notes, or marketing until its "you have a result when…" milestone is met — see
the positioning rules in moq-analyzers-docs-and-writing. All work routes
through normal change control (moq-analyzers-change-control); this skill
authorizes no shortcut.

These five directions were confirmed by the maintainer (2026-07-02) as the
places where this project can go beyond the current state of the art:
provably-zero-FP on real-world corpora, mutation-tested rules, full Moq
semantic modeling, and per-keystroke performance as a measured contract.

## Definitions (read once, used throughout)

| Term | Meaning |
|---|---|
| Analyzer | A class that runs inside the C# compiler/IDE and reports diagnostics (e.g. `Moq1002`) on consumers' code as they type. This repo ships 25 of them. |
| False positive (FP) | Analyzer flags correct code. Worst failure class: trains users to suppress rules. |
| False negative (FN) | Analyzer stays silent on wrong code. The runtime bug the rule exists to prevent ships anyway. |
| Corpus | A pinned set of real-world open-source repositories used as validation input, each identified by URL + commit SHA so runs are reproducible. |
| Mutation testing | A tool mutates production code (flips conditionals, deletes calls) and re-runs tests. A "surviving mutant" = a code change no test noticed = a semantic coverage gap. |
| Mutation score | % of mutants killed by the test suite. Complements (does not replace) block coverage. |
| BDN | BenchmarkDotNet, the benchmark harness used in `tests/Moq.Analyzers.Benchmarks/`. |
| PerfDiff | This repo's in-house tool (`src/tools/PerfDiff/`) that compares a PR's BDN results against a pinned baseline and fails CI on regression (ADR-008). |
| DIM | Default interface member — a C# 8 interface member with a body. Can be `sealed`, in which case Moq cannot override it. |
| AD0001 | The compiler diagnostic emitted when an analyzer throws; the analyzer is disabled for the rest of the session. Priority-1 failure. |
| Live-DLL harness | This repo's test infrastructure that compiles arbitrary C# against *real* Moq NuGet packages (4.8.2 and 4.18.4 via `tests/Moq.Analyzers.Test/Helpers/ReferenceAssemblyCatalog.cs`) and runs every shipped analyzer over it (`Helpers/AllAnalyzersVerifier.cs`, reflection-discovers all analyzers in namespace `Moq.Analyzers`). |
| SOTA | State of the art — what the best existing tools in this niche actually do today. |

## The five problems at a glance

| # | Problem | Anchor issues (state, 2026-07-02) | First result milestone |
|---|---|---|---|
| 1 | Corpus-based provably-zero-FP validation | none filed yet (candidate) | N-repo corpus runs clean or every hit is a filed, classified issue |
| 2 | Mutation-tested rule quality | #904 (open) | Checked-in mutation score baseline; gate for new rules |
| 3 | Sequence-pattern analyzers | #614–#617 (CLOSED as stale 2026-05-09 — not implemented) | First sequence rule beyond Moq1207 ships through full lifecycle |
| 4 | Full Moq semantic modeling | #1264, #1270 (open); #579 (closed, partially shipped as Moq1600) | Each pinned FN flips to a real diagnostic with an issue-linked fix |
| 5 | Perf as a tested contract | #1265–#1269, #594–#602 (open) | PerfDiff gate proven sound; per-rule budgets published as tested guarantees |

Recommended order if you have no other constraint: **5 → 2 → 1 → 4 → 3**.
Reason: 5 and 2 harden the measurement instruments; 1 needs a sound gate and
a trustworthy suite to interpret its output; 4 and 3 are new capability built
on top. The FP-convergence campaign (#1241–#1278) is a prerequisite stream
that runs in parallel — see moq-analyzers-fp-convergence-campaign.

---

## Problem 1 — Corpus-based provably-zero-FP validation

### Why current SOTA fails

Roslyn analyzers — including this one — are validated almost exclusively
against hand-written unit tests. FPs are therefore discovered *by users, in
production*. This repo's own history proves the cost: Moq1203 took five
separate patches (`6ec810c`, `c270302`, `894313b`, `0bef80b`, `5eec7e1`),
each triggered by a user report on a syntax shape no test anticipated.
Working hypothesis (UNVERIFIED as an exhaustive claim — run a literature/
tooling survey per moq-analyzers-research-methodology before stating it
publicly): no mocking-framework analyzer, and few Roslyn analyzers of any
kind, validate against a real-world corpus as a release gate.

### This project's specific assets

- **25 rules** (`src/Common/DiagnosticIds.cs`, lines 7–33; 21 shipped + 4
  pending in `AnalyzerReleases.Unshipped.md`) with span-exact tests — a
  mature target worth validating.
- **The live-DLL harness**: `AllAnalyzersVerifier` +
  `ReferenceAssemblyCatalog` already compile arbitrary source against real
  Moq 4.8.2/4.18.4 packages and assert "no analyzer fires". A corpus run is
  this, scaled up.
- **A proven package-injection pattern**: the `analyzer-load-test` CI job
  (`.github/workflows/main.yml:283`) downloads the locally packed nupkg,
  writes a `nuget.config` pointing at a local feed, generates a scratch
  project with `<PackageReference Include="Moq.Analyzers" Version="..."/>`,
  builds, and greps the build output. That is 80% of a corpus harness.
- **A nightly scheduled-run precedent**: `main.yml:26–27` already has
  `cron: '0 3 * * *'` for nightly perf validation; a corpus job follows the
  same shape.
- Local pack output (debug config): `artifacts/package/debug/Moq.Analyzers.<version>.nupkg`.

### First three steps in this repo

1. **Build the harness script** (new file, suggested location
   `build/scripts/corpus/Run-Corpus.ps1` to match existing script layout).
   Per corpus entry: `git clone --depth 1` at a pinned SHA, inject the local
   package, build, and collect Moq1xxx diagnostics machine-readably with
   `dotnet build /p:ErrorLog=<name>.sarif` (SARIF = the standard static-
   analysis result format). NOTE — the repo's CI does NOT currently emit
   build SARIF: `main.yml` has an upload step but nothing sets `/p:ErrorLog`,
   so no SARIF files are produced (see `moq-analyzers-diagnostics-and-tooling`
   §5, "an honest gap"). Wiring `ErrorLog` and collecting corpus SARIF is NEW
   work for this harness, not existing infrastructure. Injection mechanism is
   an **open design point**: dropping a
   `Directory.Build.props` + `nuget.config` at the clone root works only for
   repos that don't define their own; a
   `/p:CustomBeforeMicrosoftCommonTargets=<file>` import is more invasive
   but more reliable. Prototype both on one repo before scaling.
2. **Pin the seed corpus** as a checked-in manifest (suggested:
   `build/scripts/corpus/corpus.json` — array of `{url, sha, projects[]}`).
   Start with N=10 repos with verified Moq usage. Evidence that candidates
   exist: issue #617 records a GitHub code search finding 1,068 public
   occurrences of `SetupSequence`+`Returns`, naming Microsoft Semantic
   Kernel and Azure Bicep. Prefer repos that build with plain `dotnet build`.
3. **Define the triage pipeline before the first full run.** Every
   diagnostic hit gets exactly one classification: **TP** (corpus code is
   genuinely wrong — evidence the rule works; optionally report upstream) or
   **FP** (file an issue in this repo with a minimized repro; the fix ships
   with an issue-linked regression test per repo policy). Persist
   classifications in a checked-in baseline keyed by repo+SHA+rule+location
   so re-runs only surface *new* hits. Then wire it as a scheduled,
   initially non-blocking CI job modeled on the nightly perf trigger.

### You have a result when…

A pinned N-repo corpus (N ≥ 10) builds with the packed analyzer injected,
and the run ends in one of exactly two states: (a) zero unclassified
diagnostics, or (b) every hit maps to a filed issue classified TP or FP —
and the whole run is reproducible by anyone from the checked-in manifest.
**Falsifier / fallback:** if package injection cannot be made reliable
across foreign build systems, scope down to extracting corpus *files* and
compiling them through the existing test harness (`CompilationHelper` /
`AllAnalyzersVerifier`) — weaker (loses real project context) but still a
result no comparable analyzer has published.

---

## Problem 2 — Mutation-tested rule quality (issue #904)

### Why current SOTA fails

The industry gate is line/branch coverage. Issue #904 (open, filed
2026-02-18) documents the failure: Moq1203's tests had 100% coverage yet
missed the #849 FP, because code with *the same execution path but different
semantics* (literal vs. variable, `Task` vs. `Task<T>`) looks identical to a
coverage tool. This repo already requires 100% block coverage for new
analyzer code — necessary, provably insufficient. Mutation testing is the FN
counterpart of the FP campaign: it finds the places where the test suite
would not notice if analyzer logic silently broke.

### This project's specific assets

- **3,357 tests** in `tests/Moq.Analyzers.Test` with span-exact assertions
  (`{|Moq1002:...|}` markup asserts ID *and* character range) — strong
  mutant killers, unlike assertion-light suites where mutants survive
  trivially.
- **#904 is already scoped by the maintainer**: start with `src/Analyzers/`
  and `src/Common/`, run non-blocking in CI first, promote to a gate later.
- **Known ground truth to calibrate against**: issue #1270 documents dead
  test fixtures and pinned FNs — exactly the gap class mutation testing
  should rediscover. If a first run does *not* flag those areas, the
  configuration is wrong.

### First three steps in this repo

1. Add Stryker.NET to the tool manifest (it is absent today — verified
   2026-07-02 in `.config/dotnet-tools.json`):
   `dotnet tool install dotnet-stryker` from repo root, then create a
   `stryker-config.json` targeting the `Moq.Analyzers` project with
   `tests/Moq.Analyzers.Test` as the test project. Tool-manifest changes
   need a THIRD-PARTY-NOTICES review per change control.
2. **Size the runtime before committing to anything.** 3,357 tests × hundreds
   of mutants is expensive. First run: restrict to a single analyzer file
   (Stryker's mutate filter, e.g. `--mutate "**/MethodSetupShouldSpecifyReturnValueAnalyzer.cs"`)
   and record wall-clock time and mutation score. Extrapolate before scoping
   the CI job; expect to need Stryker's incremental/`since` mode for PRs.
3. Land the non-blocking CI job per #904's own plan (informational report,
   no gate), publish the per-file scores as an artifact, and file the
   baseline as a checked-in JSON. Only then propose the gate: **new analyzer
   files must meet an agreed mutation score before merge** (threshold is a
   maintainer decision, not yours — take options to the issue).

### You have a result when…

(a) A checked-in mutation-score baseline exists for `src/Analyzers/` +
`src/Common/`; (b) at least one surviving mutant has been converted into a
test that fails on the mutant and passes on real code — proof the metric
finds real gaps, not noise; (c) for the gate milestone: a PR adding a new
rule demonstrably fails CI when its tests are weakened below threshold.
**Falsifier:** if the suite kills >98% of mutants out of the box and
survivors are all equivalent mutants (mutants with no observable behavior
change), mutation testing adds cost without signal here — record that
negative result on #904 and stop.

---

## Problem 3 — Sequence-pattern analyzers (reviving #614–#617)

### Ground truth first (this is where stale context bites)

Issues #614 (gap analysis), #615 (InSequence/MockSequence coordination), #616
(incomplete sequence configuration), #617 (mixed Returns/Throws type
validation) were **closed on 2026-05-09 with "Closing due to inactivity
(6+ months stale)"** — their GitHub `state_reason` says "completed" but the
closing comments say inactivity, and the work was **not** done: `grep -rn
"MockSequence\|InSequence" src/ --include=*.cs` returns nothing
(verified 2026-07-02). What *does* exist: Moq1207
(`SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer`, shipped via
PR #581 from original request #576) validates that `SetupSequence` targets
overridable members. Everything else in the epic is unbuilt.

### Why current SOTA fails

`SetupSequence`/`MockSequence`/`InSequence` express *stateful, ordered*
mock behavior. Validating them needs (a) type-compatibility checking through
a fluent chain (`.Returns(3).Returns("x")` on an `int` method) and, for
MockSequence coordination, (b) **cross-statement analysis** — relating
several statements that share one `MockSequence` local. Shipped analyzers
almost never attempt cross-statement reasoning because of per-keystroke cost;
no mocking-framework analyzer models sequence semantics at all (working
hypothesis, UNVERIFIED — survey before claiming).

### This project's specific assets

- Moq1207's symbol plumbing: `IsMoqSetupSequenceMethod`
  (`src/Common/ISymbolExtensions.Moq.cs:60-63`) and the
  `Mock1SetupSequence` known symbol.
- The `MoqKnownSymbols` discipline (one resolution per compilation start,
  ADR-006) plus the phantom-symbol pin-test rule: every new symbol property
  gets a resolves-non-null test, because two existing properties
  (`IReturns`, `IReturns<T>`) resolve null against real Moq.
- Test fan-out across Moq 4.8.2 and 4.18.4 — sequence APIs differ between
  them, and the harness already multiplies every row across both.
- Demand evidence already gathered in #617 (1,068 public code-search hits).

### First three steps in this repo

1. **Re-file, don't resurrect silently.** Ask the maintainer to reopen
   #615–#617 or file fresh issues referencing them; refresh the #614 gap
   analysis against current `main` (Moq1207's tests and helpers moved on
   since 2025). Closed-stale issues are not authorization to start work.
2. **Build the symbol surface**: add `MockSequence` and the `InSequence`
   setup methods to `src/Common/WellKnown/MoqKnownSymbols.cs`, each with a
   resolves-non-null test against both catalog Moq versions. First verify
   the exact API shape per version with the inspection tool the repo already
   uses: `dotnet tool install -g dotnet-inspect`, then pin each version —
   `dotnet-inspect member "MockSequence" --package Moq@4.8.2 --all` and
   `... --package Moq@4.18.4 --all` (unpinned `--package Moq` inspects only the
   latest release, not the supported matrix).
3. **Ship Epic 3 first** (mixed Returns/Throws type validation, old #617):
   it is single-statement, reuses Moq1207's detection, and needs no
   cross-statement machinery — the lowest-risk proof that sequence semantics
   can be modeled. Run it through the full new-rule checklist in
   moq-analyzers-rule-lifecycle (DiagnosticIds entry, AnalyzerReleases.Unshipped.md
   row, docs/rules page, tests, per-rule benchmark file — the benchmark is
   mandatory, `tests/Moq.Analyzers.Benchmarks/Moq{Id}Benchmarks.cs`).

### You have a result when…

- **Epic-3 milestone:** a shipped rule flags
  `mock.SetupSequence(f => f.GetInt()).Returns("string")` and stays silent
  on every valid mixed `Returns`/`Throws`/async row, across both Moq
  versions, with the perf gate green.
- **Epic-1 milestone (the SOTA one):** an analyzer correctly relates two
  *separate statements* sharing a `MockSequence` instance (e.g. flags an
  `InSequence(seq)` referencing a different sequence than its siblings) —
  the first cross-statement analysis in this codebase — while the PerfDiff
  gate stays green. If cross-statement cost blows the perf budget, that
  negative result (with numbers) is itself publishable engineering data;
  record it on the issue.

---

## Problem 4 — Full Moq semantic modeling completeness

One umbrella, three concrete sub-problems, each independently falsifiable.
The end state: the analyzers' model of "what Moq can actually mock and how"
is complete enough that *truthfulness* gaps (FNs) are as systematically
hunted as FPs.

### 4a. Protected-member coverage beyond Moq1600

**State:** #579 (the protected-member spec) closed 2026-03-17. Moq1600
(`ProtectedSetupShouldUseItExprAnalyzer`) shipped and covers the core: `It`
vs `ItExpr` misuse in the string-based `Protected()` overloads that accept
matcher arguments — `Setup`, `SetupSet`, `SetupSequence`, `Verify`,
`VerifySet` (registration at
`src/Analyzers/ProtectedSetupShouldUseItExprAnalyzer.cs:69-73`).
**Open gaps from #579's acceptance criteria:** string-name validation (does
`Protected().Setup("Nam", ...)` name a real member?), overridability of the
named protected member, and `ItExpr.Ref<T>` / generic string-based setups
(Moq 4.14+). Domain trap to design around: `It.Ref<T>.IsAny` is a *nested
class* whose containing type is not `Moq.It` — naive containment checks
miss it.

**First steps:** (1) diff #579's acceptance criteria against Moq1600's
actual scope and file the delta as new issues; (2) extend the
`IProtectedMock` symbol surface in `MoqKnownSymbols` (the interface has 25
members in Moq 4.18.4 vs 14 in 4.8.2 — every addition needs version-split
tests); (3) implement string-name member validation as its own rule via the
lifecycle. **Result when:** a typo'd member name in a string-based
protected setup is flagged at compile time, with zero diagnostics on the
`DoppelgangerTestHelper` suite (user-defined Moq look-alikes must never
trigger).

### 4b. Truthfulness FNs: DIMs, statics, non-generic Setup

**State (all open, 2026-07-02):**

| FN | Where pinned | Root cause |
|---|---|---|
| Sealed default interface members treated as overridable (Moq1200/1207/1210 stay silent; Moq throws at runtime) | issue #1264 | overridability check doesn't model DIM sealing |
| Void-method setups never analyzed (any rule keyed on `IsMoqSetupMethod`) | #1270, as commented "pins current behavior" test rows | `IsMoqSetupMethod` requires `IsGenericMethod` (`src/Common/ISymbolExtensions.Moq.cs:40-43`), and the void overload `Setup(Expression<Action<T>>)` is non-generic |
| Method-group and custom-delegate callbacks not analyzed by Moq1100 | #1270 commented rows | only lambda/delegate-creation syntax inspected |

**First steps:** (1) land #1270 first — it pins today's behavior as
executable rows, so every later fix is a visible row-flip, not an invisible
behavior change; (2) fix #1264 (smallest semantic delta); (3) design the
non-generic-Setup fix with the FP-convergence escape-hatch method — it
widens the analyzed surface of many rules at once, so it needs the full
adversarial test axis (parentheses, chaining, extension-method wrapping).
**Result when:** each commented pinned-FN row in #1270 has flipped to a
real `{|MoqXXXX:...|}` assertion via an issue-linked fix, with zero new FPs
in the `AllAnalyzersVerifier` suites.

### 4c. MockBehavior data-flow analysis (Moq1400/Moq1410)

**State:** the limitation is documented in the source, verbatim
(`src/Analyzers/SetExplicitMockBehaviorAnalyzer.cs:53-54`):

```csharp
// NOTE: This logic can't handle indirection (e.g. var x = MockBehavior.Default; new Mock(x);). We can't use the constant value either,
// as Loose and Default share the same enum value: `1`. Being more accurate I believe requires data flow analysis.
```

So `var b = MockBehavior.Default; new Mock<IFoo>(b);` is an FN today, and
constant-value comparison can never fix it because `Loose` and `Default`
are both `1` — only tracking *which field reference* flowed into the
argument works.

**First steps:** (1) prototype conservative single-method tracking: when
the behavior argument is a local, walk its declarator/assignments in the
containing method and flag only if **every** reaching value is a
`MockBehavior.Default` field reference (conservative = FN-safe, never
FP-risky); (2) benchmark the prototype with the existing per-rule
benchmarks (`tests/Moq.Analyzers.Benchmarks/Moq1400ExplicitBehaviorBenchmarks.cs`,
`Moq1410StrictBehaviorBenchmarks.cs`) — Roslyn's `ControlFlowGraph`-based
dataflow is a candidate but is expensive per keystroke, so measure before
adopting; (3) note the related open correctness issue #1255 (boxed-enum
`==` comparison in the MockBehavior analyzers) and fix it first so the new
logic builds on sound equality. **Result when:** the indirection example
above is flagged, a conditionally-reassigned local is *not* flagged, and
the PerfDiff gate shows an accepted delta on the two benchmark files.

---

## Problem 5 — Performance as a tested contract

### Why current SOTA fails

Analyzers run on every keystroke in consumers' IDEs, yet analyzer projects
almost never measure that cost, let alone gate on it. This repo is ahead —
it has per-rule BDN benchmarks (one `Moq{Id}Benchmarks.cs` per rule in
`tests/Moq.Analyzers.Benchmarks/`), a diff tool (PerfDiff), a pinned
baseline (`build/perf/baseline.json`, currently release 0.1.1 / SHA
`0cbc088`), a required PR gate (`build/scripts/perf/ComparePerfResults.ps1:24`
runs PerfDiff with `--failOnRegression`; ADR-008), a PR fast path (filter
`'*(FileCount: 1)'`) and a nightly full run (filter `'*'`,
`main.yml:507-515`, cron `main.yml:26-27`). **But the 2026-07-02 audit
proved the gate is not yet sound**, so none of this is claimable as a
guarantee today.

### The gate's known holes (all filed, all open, 2026-07-02)

Five PerfDiff correctness defects — #1265 (ETL veto disarms the gate), #1266
(mismatched benchmark sets silently intersected), #1267 (absolute budgets
never read the baseline), #1268 (infinite ratios dropped from the verdict), #1269
(degenerate-input crashes/skips) — mean the gate can pass green while a
real regression ships, or block an innocent PR. The canonical per-strategy
threshold/defect table lives in **moq-analyzers-diagnostics-and-tooling §2**;
re-derive thresholds from `grep -rn 'Threshold' src/tools/PerfDiff/BDN/Regression/`.
This problem exists to make that defect table obsolete.

Supporting backlog: PerfDiff unit/integration tests #594–#601 (the tool that
gates every PR has almost no tests of its own — 4 today) and threshold
externalization #602 (budgets are hard-coded magic numbers).

### First three steps in this repo

1. Fix #1265–#1269 **in that order** (gate integrity before anything is
   published). Each fix lands with regression tests, which forces progress
   on #594–#601 at the same time.
2. Land #602: externalize thresholds into configuration so "the budget" is
   a reviewable, versioned artifact instead of constants in strategy
   classes.
3. **Prove the gate before publishing it**: plant an intentional 2×
   slowdown in one analyzer on a scratch branch, run the pipeline
   (`./Perf.sh -diff` locally; `FORCE_PERF_BASELINE=true` forces a baseline
   re-run), and confirm the perf job fails. A gate that has never caught a
   planted regression is untested infrastructure. Then draft per-rule
   budget documentation (candidate starting values: the existing 250 ms P95
   / 100 ms mean, made baseline-aware) for maintainer sign-off.

### You have a result when…

(a) All five PerfDiff issues are closed with tests; (b) a planted 2×
regression demonstrably fails CI and reverting it passes — recorded as
evidence on the PR; (c) per-rule budgets are documented and every statement
of the form "this analyzer costs ≤ X per keystroke-equivalent compile" in
docs traces to a gated benchmark. Until (a)–(c), the only honest public
sentence is "performance is benchmarked and diffed in CI" — not
"guaranteed".

---

## Cross-cutting rules for all five problems

- **Label discipline:** every artifact produced under this skill carries
  `open` / `candidate` / `hypothesis` status until its milestone is met.
  "First analyzer to X" and "provably zero FP" are banned phrases in
  shipped docs until verified per moq-analyzers-research-methodology and
  cleared under moq-analyzers-docs-and-writing positioning rules.
- **Milestones are falsifiable on purpose.** If a step disproves the
  premise (e.g. mutation testing finds only equivalent mutants; cross-
  statement analysis can't meet the perf budget), the negative result gets
  recorded on the anchor issue. A documented dead end beats an abandoned
  branch — this repo's history has both (`copilot/fix-496` abandoned
  silently vs. `5172cf3` documenting a failed fallback removal that guided
  the later successful one).
- **No new rule outside the lifecycle.** Problems 3 and 4 produce new
  analyzers; every one goes through the checklist in
  moq-analyzers-rule-lifecycle (ID range table, Unshipped.md row, docs
  page, benchmark file, doppelganger tests). Research status exempts
  nothing.
- **Issue hygiene:** anchor issue states drift. Re-verify before starting
  (commands in Provenance below); note that GitHub's `state_reason:
  completed` on #614–#617 is misleading — read the closing comments.

## When NOT to use this skill

| If you are… | Use instead |
|---|---|
| Fixing a reported FP/FN or working #1241–#1278 | moq-analyzers-fp-convergence-campaign |
| Root-causing one bug or crash | moq-analyzers-debugging-playbook |
| Shipping a new rule whose scope is already agreed | moq-analyzers-rule-lifecycle |
| Designing/running an experiment rigorously, or vetting a "first ever" claim | moq-analyzers-research-methodology |
| Proving a single claim about Moq/analyzer behavior | moq-analyzers-proof-toolkit |
| Looking up Roslyn APIs or Moq API shapes | roslyn-analyzer-reference, moq-api-reference |
| Setting up build/test environment, or CI/config questions | moq-analyzers-build-and-env, moq-analyzers-config-and-flags |
| Writing tests/validation for in-scope work | moq-analyzers-validation-and-qa |
| PR mechanics, evidence requirements, merge gates | moq-analyzers-change-control |
| Architecture invariants (ADRs, banned APIs) | moq-analyzers-architecture-contract |
| Past-incident history | moq-analyzers-failure-archaeology |
| Benchmarks/PerfDiff mechanics as tooling (not as research direction) | moq-analyzers-diagnostics-and-tooling |
| Writing docs or public positioning | moq-analyzers-docs-and-writing |
| .NET API design review of new public surface | dotnet-api-design-standards |

## Provenance and maintenance

Re-verify before relying on any volatile claim above:

- Commit context: `git log -1 --format='%h %s'` (expect `05135b2` lineage or later).
- Rule count (25): `grep -c '= "Moq' src/Common/DiagnosticIds.cs`
- Issue states (use GitHub UI/API; `gh` is not installed in all sandboxes): check #904, #615–#617, #1264, #1265–#1269, #1270, #594–#602 at <https://github.com/rjmurillo/moq.analyzers/issues> — and read closing comments, not just `state_reason`.
- Sequence work still unbuilt: `grep -rn "MockSequence\|InSequence" src/ --include=*.cs` (expect no hits).
- Non-generic Setup FN root cause: `grep -n "IsGenericMethod" src/Common/ISymbolExtensions.Moq.cs`
- MockBehavior data-flow limitation comment: `grep -n "data flow analysis" src/Analyzers/SetExplicitMockBehaviorAnalyzer.cs`
- Moq1600 protected scope: `grep -n "AddStringOverloads" src/Analyzers/ProtectedSetupShouldUseItExprAnalyzer.cs`
- PerfDiff budgets: `grep -n "250ms\|100ms" src/tools/PerfDiff/BDN/Regression/PercentileRegressionStrategy.cs src/tools/PerfDiff/BDN/Regression/MeanWallClockRegressionStrategy.cs`
- Perf gate invocation: `grep -n "failOnRegression" build/scripts/perf/ComparePerfResults.ps1`
- Nightly cron + perf filters: `grep -n "cron\|FileCount" .github/workflows/main.yml`
- Perf baseline pin: `cat build/perf/baseline.json`
- Stryker still absent: `grep -i stryker .config/dotnet-tools.json` (expect no hits until Problem 2 step 1 lands).
- Test count: `dotnet test --settings ./build/targets/tests/test.runsettings` (3,357 in Moq.Analyzers.Test as of 2026-07-02; 2 PackageTests failures are sandbox-remote-URL artifacts, not defects).
- Packed nupkg path: `ls artifacts/package/*/Moq.Analyzers.*.nupkg` (after `dotnet build`).

Last verified: 2026-07-02 against commit 05135b2.
