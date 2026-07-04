---
name: moq-analyzers-research-methodology
description: The discipline that turns a hunch into an accepted result in moq.analyzers — the evidence bar (one mechanism must explain ALL observations including negatives), assigned adversarial refutation (the author is never the only tester), predict-numbers-before-running for perf and FP work, the idea lifecycle, and experiment hygiene. Load when proposing a new analyzer idea or fix hypothesis, designing an experiment or corpus study, judging whether a claimed root cause is real, deciding how to write up or retire an investigation, or when someone says "I found the cause", "let's try", "this should be faster", or "can we delete this rule". Do NOT load for executing an already-planned fix (moq-analyzers-rule-lifecycle for new rules, moq-analyzers-fp-convergence-campaign for the backlog), single-claim verification (moq-analyzers-proof-toolkit), picking which open problem to attack (moq-analyzers-research-frontier), or the incident chronicle (moq-analyzers-failure-archaeology).
---

# Research methodology: from hunch to accepted result

This skill defines HOW an idea becomes an accepted result in this repository. It is
method, not backlog: for WHAT to work on, see `moq-analyzers-research-frontier`; for
executing a planned fix, see `moq-analyzers-fp-convergence-campaign` and
`moq-analyzers-rule-lifecycle`.

Context for zero-context readers: this repo ships Roslyn analyzers (compiler plugins
that inspect C# code as it is typed) for the Moq mocking library. They run inside
customers' compilers and IDEs in mission-critical codebases. A wrong "fix" here is a
false positive (FP: diagnostic on correct code) or false negative (FN: silence on
broken code) shipped to thousands of builds. The costliest historical failure mode is
plausible-but-wrong code — changes that look correct, pass the reported repro, and are
wrong about the mechanism. This methodology exists to catch exactly that.

Every date-stamped claim below was re-verified 2026-07-02 against commit `05135b2`.

---

## 1. The evidence bar: one mechanism must explain ALL observations

A **mechanism** is the single causal statement that explains why the bug fires on every
failing input AND stays silent on every passing input. "This repro no longer triggers"
is not a mechanism; it is one data point.

**The rule:** before you write a fix, write one sentence of the form
"X happens because Y, therefore it fires on {A, B, C} and NOT on {D, E}" — and check
that sentence against the negatives. If your explanation does not predict the cases
that do NOT reproduce, you have found *a* trigger, not *the* mechanism.

### Negative worked example: the Moq1203 whack-a-mole (5 patches)

Moq1203 (`MethodSetupShouldSpecifyReturnValueAnalyzer`) flags `mock.Setup(...)` calls
on value-returning members that never specify a return value. Each patch fixed only
the syntactic shape from the report in front of it, so the FP class kept finding new
escape hatches: fluent chaining (`6ec810c`, #886) → parentheses (`c270302`, #895) →
the same hole in sibling rules Moq1100/Moq1206 (`894313b`, #907) → delegate-based
overload resolution (`0bef80b`, #919) → user extension methods (`5eec7e1`, #1086).
Full incident record with dates, originating issues, and wrapper details:
moq-analyzers-failure-archaeology entry 2.

Three months of releases with a known-leaky rule. The durable lesson (now doctrine):
**syntactic wrappers — parentheses, extension methods, fluent chains — are one family
of escape hatch**; a fix that handles one wrapper and not the family has not named the
mechanism. And it is still not fully closed: the 2026-07-02 audit found the fifth
patch's string-name fallback itself overreaches (fires even when the symbol resolved to
a non-Moq method — FN, filed as issue #1243, open as of 2026-07-02).

### Positive worked example: Moq1302 #1010 — name the mechanism, close the class

User report #1010 (2026-03-06): Moq1302 flagged `Mock.Of<IResponse>(r => r.Status ==
StatusCodes.Status200OK)` — a canonical LINQ-to-Mocks expression. The fix `4b705e2`
(PR #1017, merged same week, milestone v0.4.2) named the mechanism in the commit
message:

> **Root cause**: The analyzer walked both sides of binary expressions without
> checking whether the member access originated from the lambda parameter. Static
> members, constants, and external instance properties were incorrectly flagged as
> invalid mock members.

The mechanism — *a member access not rooted in the lambda parameter is a value
expression, not a mock-member specification* — predicted the entire class: static
consts, enum values, captured locals, method parameters, instance fields on `this`,
multi-hop property chains, both `==` and `!=`, string concatenation, `&&`/`||`
combinations. PR #1017 shipped 16 boundary tests; follow-up `3399297` (PR #1020,
issue-linked regression suite) added 10 more scenarios covering every one of those
predictions, on both Moq 4.8.2 and 4.18.4. Result: zero user-reported Moq1302 FPs
since the fix (verified by issue search, 2026-07-02 — the only later Moq1302 issues
are audit-filed robustness items #1261/#1264, not FP reports).

### Mechanism checklist (fill in before coding)

- [ ] One sentence: "fires because ___".
- [ ] List of known-failing inputs; the sentence explains each.
- [ ] List of known-PASSING near-miss inputs; the sentence explains why each passes.
- [ ] The enumerable *class* of inputs the fix will cover (not just the repro).
- [ ] What the fix deliberately does NOT cover (accepted FNs), written down.

---

## 2. Assigned adversarial refutation: the shared-blind-spot protocol

**The rule (maintainer doctrine, confirmed 2026-07-02): whoever wrote the code — human
or model — does not get to be its only tester.** A second party (different human, or at
minimum a different session with no access to the implementation rationale) writes
boundary cases explicitly trying to BREAK the change, before merge.

Why: an author's tests encode the author's model of the problem. If the model has a
hole, the implementation AND its tests share that hole. This repo has a concrete
instance: the Moq1302 analyzer and its original test suite were both written by the
same AI agent in one PR (#511, merged 2025-06-25); the suite covered virtual vs
non-virtual members but had no rows for static members, constants, or captured locals
on the comparison right-hand side — and eight months later user report #1010 hit
exactly that shared blind spot. The scenarios that would have caught it are precisely
the ones a second party later added in the #1020 regression suite.

### The standard refutation axes

The adversary's job is mechanical, not creative. For any analyzer/fix touching
expression analysis, write at least one breaking attempt per axis:

| Axis | Boundary cases to write |
|---|---|
| Operand kind | literal (`42`, `"x"`), local variable, `static` member, `const`, method call, captured local, method parameter, instance field on `this` |
| Syntactic wrapper | parenthesized expression, user extension method, fluent chain reordering, target-typed `new`, nested lambda |
| Symbol resolution state | resolves to Moq symbol; resolves to NON-Moq symbol with a Moq-like name; overload-resolution failure (candidates only); no symbol at all (mid-edit code) |
| Moq version | 4.8.2 AND 4.18.4 (`WithOld/New/BothMoqReferenceAssemblyGroups()` fan-out — many APIs differ between them) |
| Malformed code | incomplete/uncompilable source (analyzer priority #1 is "never crash"; the audit found only 2 of 10 sampled test classes cover this) |
| Look-alikes | user-defined `Mock<T>`/`MockBehavior` doppelgangers must NOT trigger (`DoppelgangerTestHelper`) |

Test-authoring mechanics (markup syntax, fan-out helpers) are in
`moq-analyzers-validation-and-qa`; this skill only fixes WHO writes them and WHEN
(second party, pre-merge).

### Refutation also applies to proposals, not just code

Worked example — PR #504 (branch `copilot/fix-496`, 2025-06-18): an AI agent proposed
a new "InSequence must receive a valid MockSequence" analyzer. The maintainer refuted
it by *trying to construct any compilable input that could ever trigger it*:

> "I don't think `HasValidMockSequenceParameter` can ever be legitimately hit.
> Everything I've tried results in an error caught by the compiler, making the
> analyzer redundant. ... The only way the analyzer would *ever* report a diagnostic
> is if the code is already broken and would not compile."

Every attempted trigger (`InSequence()` with 0/2 args, wrong argument type) was a
compiler error. The diagnostic's reachable trigger set, restricted to compilable code,
was empty — so the rule had no value. This is the template: **refute by construction.
Enumerate the inputs that would have to exist for the claim to matter, and try to
build them.** If you cannot build one that compiles, the proposal dies (in writing —
see section 4).

### Running a refutation session (commands)

```bash
export PATH="$HOME/.dotnet:$PATH"

# Run only the suite under attack while iterating on breaking cases:
dotnet test --settings ./build/targets/tests/test.runsettings \
  --filter "FullyQualifiedName~LinqToMocksExpressionShouldBeValid"

# Check the actual Moq API surface before asserting an overload exists
# (see moq-analyzers-proof-toolkit for the full recipe):
dotnet tool install -g dotnet-inspect   # once
dotnet-inspect member "IReturns<TMock,TResult>" --package Moq@4.18.4 --all  # pin @version (re-run @4.8.2); unpinned = latest only
```

A breaking case that survives becomes an issue-linked regression test row (repo rule:
every FP/FN fix ships with one). A breaking case that kills the change saved a release.

---

## 3. Hypothesis predicts numbers BEFORE the run

State the expected measurement before you take it. If you only interpret numbers after
seeing them, every outcome can be rationalized as success.

### Performance work: predict the delta against the real gates

PerfDiff is the CI perf gate (ADR-008, required check): it re-runs BenchmarkDotNet
micro-benchmarks at a pinned baseline commit (`build/perf/baseline.json`) and at your
PR, then compares via five strategies. Before running, write down which strategy you
expect to move and by how much — a prediction requires knowing the current thresholds,
which are code constants under active repair (#1265–#1269): the canonical
strategy/threshold/defect table is **moq-analyzers-diagnostics-and-tooling §2**;
re-derive from `grep -rn 'Threshold' src/tools/PerfDiff/BDN/Regression/` before
predicting against them.

CAVEAT (2026-07-02): the audit found this gate has integrity holes — the ETL path can
veto genuine regressions as "noise", mismatched benchmark sets silently intersect, the
absolute-budget strategies never consult the baseline, and infinite ratios are dropped
from the verdict (filed as #1265–#1269). Until those land, treat a green perf check as
weak evidence and your stated prediction + local run as the primary evidence.

Worked example: the per-compilation caching fixes `9febdda` (#1026) and `3b5ac71`
(#1033) stated the mechanism *quantified, in the PR, before the gate ran*: moving
`MoqKnownSymbols` construction from per-operation callbacks to
`RegisterCompilationStartAction` "eliminates redundant allocation of 53 `Lazy<T>`
fields on every operation action invocation" (#1026, 3 analyzers; #1033 repeated it
for 9 more). The predicted observable — allocation elimination proportional to
operation count, zero behavior change ("only the lifetime ... changed") — was checkable
against the run, not fitted to it.

```bash
# Local perf comparison against the pinned baseline (invoke via pwsh — PerfCore.ps1
# has no shebang and is not executable, so a bare ./ call fails on Linux/macOS):
pwsh build/scripts/perf/PerfCore.ps1 -projects \
  "tests/Moq.Analyzers.Benchmarks/Moq.Analyzers.Benchmarks.csproj" -diff
```

### FP/FN work: predict exactly which test rows flip

Before touching the analyzer, write the table: which existing rows stay green (and
WHY, per row group), which new rows go from missing-diagnostic to flagged, which go
from flagged to silent. Gold-standard example in this repo: issue #1243 (open,
2026-07-02) — its "Why every existing test still passes" section walks every data
source of `MethodSetupShouldSpecifyReturnValueAnalyzerTests` (`TestData`,
`OverloadResolutionFailureTestData`, `CustomReturnTypeTestData` delegate rows,
`Issue1067_*` suites) and states per-group why the new predicate leaves it unchanged,
then lists the exact new positive rows. If you cannot write that table, you do not yet
know your mechanism (section 1).

Issue #1264 shows the same discipline for empirical inputs: a "Verified facts" table of
Roslyn symbol flags (obtained by compiling probe code against the pinned Roslyn 4.8)
and Moq runtime behavior (Moq 4.18.4 + Castle 5.1.1 actually executed), recorded
BEFORE proposing the fix. Copy that shape: claims about Roslyn or Moq behavior are
measured, dated, and version-pinned, never recalled from memory.

---

## 4. The idea lifecycle observed in this repo

Every accepted result — and every rejected one — leaves a written trail. The stages,
with real artifacts:

| Stage | Artifact | Real examples (states as of 2026-07-02) |
|---|---|---|
| 1. Trigger | user issue / audit finding / version-diff observation | #849, #1010 (user FP reports); #1241–#1278 (audit batch) |
| 2. Explainer | issue titled `Explainer: <topic>` — teaches the problem before anyone codes | #118 "Refactor to IOperation" (closed), #627 "What is CRAP and Why Reduce It?" (open), #639, #687, #767, #774 |
| 3. Task breakdown | companion issue `Tasks for Explainer: <topic>`, or Gap-Analysis → Epics | #645, #694, #647; sequence patterns: #614 (gap analysis) → Epics #615/#616/#617 |
| 4. Implementation | PR with evidence blocks (format/build/test/coverage output pasted in the body; Moq-version note) — see CONTRIBUTING.md "Validation Evidence Requirements" | #1017 fix + #1020 regression-suite PR pair |
| 5. Ship | squash merge → release-drafter release notes → `AnalyzerReleases.Unshipped.md` promotion at release | v0.4.2 carried the #1010 fix |
| — OR — Retirement | proposal closed WITH written rationale; branch left for archaeology | PR #504: "After investigation, there's no real value for this analyzer and changes." |

Rules this table implies:

- **Big ideas get an Explainer before they get code.** The Explainer states the
  problem, the mechanism, and the success metrics (see #614's acceptance-criteria /
  INVEST format). Audit-batch issues (#1241–#1278) show the mature form: current
  behavior with file:line quotes, desired behavior, implementation plan, predicted
  test outcomes, constraints (which ADRs apply), validation commands, out-of-scope.
- **Retirement is written down, not ghosted.** A dead idea gets a closing comment
  naming the refutation (PR #504's is quoted in section 2). The branch stays
  (`copilot/fix-496` still exists) so the next person can find WHY it died —
  `moq-analyzers-failure-archaeology` catalogs these dead branches. An abandoned
  branch with no written verdict is a methodology failure even if abandoning was
  right.
- **Do not re-litigate a written retirement without new evidence.** If you rediscover
  a retired idea, your first artifact is "what changed since the rejection", not code.

```bash
# Find whether an idea has prior art before proposing it:
git log --all --oneline --grep="<keyword>"
git branch -r | grep -i "fix-\|feature/"       # AI spikes live on copilot/fix-*
# Issue search: https://github.com/rjmurillo/moq.analyzers/issues?q=<keyword>
```

---

## 5. Where good ideas historically came from

When hunting for the next result, these four sources have actually produced merged
work here — prefer them over invention:

| Source | Mechanism | Verified examples |
|---|---|---|
| User FP/FN reports | a real codebase hits a case the suite never encoded; the report IS the missing test row | #849 → the entire Moq1203 hardening line; #1010 → Moq1302 `IsRootedInLambdaParameter` |
| Systematic audits | read the code adversarially, file everything with a plan | 2026-07-02 line-by-line audit: 54 findings → 32 implementation-ready issues #1241–#1278 (crash, FP/FN, perf-gate integrity, CI security) |
| Dogfooding the toolchain | run the project's own probes against its assumptions | `dotnet-inspect` against the shipped Moq DLLs exposed phantom `IReturns` symbols in `MoqKnownSymbols` (properties that resolve null — pinned by `MoqKnownSymbolsTests`) and the untracked `Moq.GeneratedReturnsExtensions` class → issue #1243 |
| Upstream Moq version diffs | diff the API surface between supported versions; every asymmetry is a candidate rule or a version-gated test | 4.8.2 vs 4.18.4: 19 vs 20 `Returns` overloads, 14 vs 25 `IProtectedMock` members, missing `SetupAdd`/`SetupRemove` — drives the mandatory two-version test fan-out and rules like Moq1600 (`ItExpr` in protected setups) |

Candidate source (unproven here, labeled open): real-world corpus scanning for
provably-zero-FP claims — scoped in `moq-analyzers-research-frontier`, not yet an
established pipeline in this repo (2026-07-02).

---

## 6. Experiment hygiene in a mission-critical repo

Analyzers execute inside customers' compilers. There is no "try it in prod".

| Rule | Practice |
|---|---|
| Experiments never ship default-on | Exploratory behavior lives in test-only surfaces (new test rows, benchmark projects, probe code under `tests/`) or in **draft PRs** — never behind a shipped default-enabled diagnostic without the full gate chain (build 0-warnings under `/p:PedanticMode=true`, full test suite, perf gate, analyzer-load matrix; see `moq-analyzers-change-control`). This skill describes no way around change control; there isn't one. |
| Spikes are labeled as spikes | Historical AI spike branches are `copilot/fix-*`; they are inputs to human PRs (e.g. `copilot/fix-634`'s detection ideas landed via human PR #770), not merge candidates themselves. |
| Findings are date-stamped and version-pinned | "Moq 4.18.4 has 20 `Returns` overloads (measured 2026-07-02 via dotnet-inspect)" — because Moq versions, Roslyn pins (4.8, ADR-003), and test counts all drift. An undated finding is a future plausible-but-wrong claim. |
| Writing it down is part of DONE | Per `.github/copilot-instructions.md`: persist new verified findings to the project memory files and update `docs/` where affected, before yielding. A result that lives only in a chat transcript or your head does not exist. Every FP/FN fix ships its issue-linked regression test in the same PR. |
| Negative results are results | The failed fallback-removal attempt was documented in `5172cf3` ("investigate and document IsMoqRaisesMethod fallback removal blockers", #768) BEFORE the successful removal `35d363d` (#770). Writing down why it could not be done yet is what made the later success safe. |
| Compilable test code only | Never write test rows that would not compile (e.g. setups on static/const members) — the compiler already rejects them, so they prove nothing (PR #504's lesson) and violate the repo test rules. |

---

## When NOT to use this skill

| If the task is... | Use instead |
|---|---|
| Executing a planned correctness-backlog fix (#1241–#1264, #1270) | `moq-analyzers-fp-convergence-campaign` |
| Adding/changing/shipping a rule end-to-end | `moq-analyzers-rule-lifecycle` |
| Verifying one specific claim (overload exists, span lands, symbol resolves) | `moq-analyzers-proof-toolkit` |
| Choosing which frontier problem to attack | `moq-analyzers-research-frontier` |
| Looking up what was tried before / dead branches | `moq-analyzers-failure-archaeology` |
| Test markup mechanics, coverage evidence format | `moq-analyzers-validation-and-qa` |
| Root-causing a single failing test or crash | `moq-analyzers-debugging-playbook` |
| PR gates, branch naming, commit format, what may change | `moq-analyzers-change-control` |
| Build/CLI/environment setup | `moq-analyzers-build-and-env` |
| Roslyn API concepts (IOperation, symbols, spans) | `roslyn-analyzer-reference` |
| Moq API ground truth (overloads, versions, fluent types) | `moq-api-reference` |
| Architecture invariants (ADRs, banned APIs) | `moq-analyzers-architecture-contract` |
| PerfDiff/benchmark tooling mechanics and output reading | `moq-analyzers-diagnostics-and-tooling` |
| Writing rule docs / release notes | `moq-analyzers-docs-and-writing` |
| BCL/API design standards for public surface | `dotnet-api-design-standards` |
| Analyzer config, severity, editorconfig flags | `moq-analyzers-config-and-flags` |

## Pre-merge methodology checklist

- [ ] Mechanism sentence written; explains all positives AND all negatives (section 1).
- [ ] Predicted test-row table (FP/FN work) or predicted perf delta vs named thresholds (perf work) written BEFORE the run (section 3).
- [ ] A party other than the author has attacked it along the standard axes: literal / local / static / const / method call / wrappers / both Moq versions / malformed code (section 2).
- [ ] Every surviving breaking case is an issue-linked regression test in the PR.
- [ ] PR body carries the evidence blocks (CONTRIBUTING.md "Validation Evidence Requirements") and a Moq-version note.
- [ ] Findings date-stamped; docs/memory updates included; if the idea died, the rationale is posted where the next person will find it (section 4).

## Provenance and maintenance

- Moq1203 saga commits: `git log --format="%h %ad %s" --date=short 6ec810c c270302 894313b 0bef80b 5eec7e1 --no-walk`
- Moq1302 fix + suite: `git log --format="%h %ad %s" --date=short 4b705e2 3399297 --no-walk`; root-cause text: `git log -1 4b705e2 --format=%B`
- Perf-fix quantified claims: `git log -1 9febdda --format=%B | grep '53'` (the commit body reads "53 \`Lazy<T>\` fields" — a backtick sits between "53" and "Lazy", so don't grep for "53 Lazy"); `git log -1 3b5ac71 --format=%B`
- PR #504 retirement rationale: <https://github.com/rjmurillo/moq.analyzers/pull/504> (closing comments, 2025-06-18); branch: `git log origin/copilot/fix-496 --oneline -3`
- Explainer/Epic issue states (may close/reopen): <https://github.com/rjmurillo/moq.analyzers/issues?q=Explainer+in%3Atitle> and issues #614–#617, #627
- Open-issue states cited (#1243, #1250, #1261, #1264, #1265–#1269, #1270): check each on GitHub before relying on "open"
- PerfDiff thresholds: `grep -rn "Threshold.Parse\|ThresholdValueNs" src/tools/PerfDiff/BDN/Regression/`
- Perf baseline pin: `cat build/perf/baseline.json`
- Evidence requirements: `grep -n "Validation Evidence Requirements" CONTRIBUTING.md`
- DONE-includes-memory rule: `grep -n "memories" .github/copilot-instructions.md`
- "Zero Moq1302 FP reports since fix": search `repo:rjmurillo/moq.analyzers Moq1302 created:>2026-03-08` (only audit items #1261/#1264 as of 2026-07-02)
- Last verified: 2026-07-02 against commit 05135b2
